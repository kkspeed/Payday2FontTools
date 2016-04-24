(* Copyright 2016, kkspeed
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *)

namespace Payday2FontTools

open FSharp.Data
open System
open System.Collections.Generic
open System.IO

module FontIO =
    module BMFont =
        [<Literal>]
        let BMSample = """<?xml version="1.0"?>
<font>
  <info face="WenQuanYi Zen Hei" size="-15" bold="0" italic="0"
     charset="" unicode="1" stretchH="100" smooth="1" aa="1"
     padding="0,0,0,0" spacing="1,1" outline="0"/>
  <common lineHeight="18" base="14" scaleW="2048" scaleH="2048" pages="1"
     packed="0" alphaChnl="0" redChnl="4" greenChnl="4" blueChnl="4"/>
  <pages>
    <page id="1" file="output_test.dds_0.dds" />
    <page id="2" file="output_test.dds_1.dds" />
  </pages>
  <chars count="223">
    <char id="0" x="1686" y="0" width="1" height="1" xoffset="0" yoffset="0"
       xadvance="0" page="0" chnl="15" />
    <char id="1" x="1736" y="0" width="1" height="1" xoffset="0" yoffset="0"
       xadvance="0" page="0" chnl="15" />
    <char id="2" x="1724" y="0" width="1" height="1" xoffset="0" yoffset="0"
       xadvance="0" page="0" chnl="15" />
    <char id="3" x="1734" y="0" width="1" height="1" xoffset="0" yoffset="0"
       xadvance="0" page="0" chnl="15" />
  </chars>
</font>"""
        type BmFont = XmlProvider<BMSample>
        let ReadBMFont (fontFile: string) =
            let path = Path.GetDirectoryName fontFile
            let fontSpec = BmFont.Load fontFile
            let pages = [ for p in fontSpec.Pages ->
                              (p.Id, new ImageMagick.MagickImage (
                                         Path.Combine (path, p.File))) ]
                      |> Map.ofList
            fontSpec.Chars.Chars
             |> Seq.groupBy (fun c -> c.Page)
             |> Seq.map (fun (p, cs) ->
                         { FontRep.image = pages.[p]
                         ; FontRep.chars =
                           cs |> Seq.map (fun c ->
                              { unicode = c.Id
                              ; width = uint8 c.Width
                              ; height = uint8 c.Height
                              ; xadvance = int8 c.Xadvance
                              ; xoffset = int8 c.Xoffset
                              ; yoffset = int8 c.Yoffset
                              ; x = uint16 c.X
                              ; y = uint16 c.Y})})
             |> Seq.reduce FontRep.(<->)

    module DisselFont =
        type FontProperty = {
            width : uint8;
            height : uint8;
            xadvance : int8;
            xoffset : int8;
            yoffset : int8;
            x : uint16;
            y : uint16
        }
        let ReadDisselFont fontFile =
            let imageFile = Path.GetFileNameWithoutExtension fontFile + ".dds"
            let imagePath = Path.Combine (Path.GetDirectoryName fontFile, imageFile)
            let image = new ImageMagick.MagickImage (imagePath)
            use stream = File.Open(fontFile, FileMode.Open, FileAccess.Read, FileShare.Read)
            let reader = new BinaryReader (stream)
            let ignoreBytes = reader.ReadBytes >> ignore
            let readHeader () =
                let numChars = reader.ReadInt32 ()
                ignoreBytes 4
                let part2Offset = reader.ReadInt32 ()
                ignoreBytes 8  // Ignore the non-sense region.
                ignoreBytes 8  // Ignore the redundant # of characters.
                let part3Offset = reader.ReadInt32 ()
                (numChars, part2Offset, part3Offset)
            let readPart2 num offset =
                reader.BaseStream.Seek (offset, SeekOrigin.Begin) |> ignore
                [ for i in 1 .. num ->
                  ignoreBytes 1
                  let width = reader.ReadByte ()
                  let height = reader.ReadByte ()
                  let xadv = reader.ReadSByte ()
                  let xoffset = reader.ReadSByte ()
                  let yoffset = reader.ReadSByte ()
                  let x = reader.ReadUInt16 ()
                  let y = reader.ReadUInt16 ()
                  { width = width
                    ; height = height
                    ; xadvance = xadv
                    ; xoffset = xoffset
                    ; yoffset = yoffset
                    ; x = x
                    ; y = y } ]
            let readPart3 num offset =
                reader.BaseStream.Seek (offset, SeekOrigin.Begin) |> ignore
                [ for i in 1 .. num ->
                  let unicode: int32 = reader.ReadInt32 ()
                  ignoreBytes 4
                  unicode ]
            let (numChars, part2Offset, part3Offset) = readHeader ()
            let fps = readPart2 numChars (int64 part2Offset)
            let us = readPart3 numChars (int64 part3Offset)
            { FontRep.image = image
            ; FontRep.chars = seq { for (fp, u) in List.zip fps us ->
                                    { FontRep.width = fp.width
                                    ; FontRep.height = fp.height
                                    ; FontRep.unicode = u
                                    ; FontRep.xoffset = fp.xoffset
                                    ; FontRep.yoffset = fp.yoffset
                                    ; FontRep.xadvance = fp.xadvance
                                    ; FontRep.x = fp.x
                                    ; FontRep.y = fp.y }} }

        let WriteDisselFont outFile (font: FontRep.Font) =
            let outImage = Path.Combine (Path.GetDirectoryName outFile,
                               Path.GetFileNameWithoutExtension outFile + ".dds")
            font.image.Write outImage
            let width = font.image.Width
            let height = font.image.Height
            use stream = new FileStream (outFile, FileMode.Create, FileAccess.Write)
            use writer = new BinaryWriter (stream)
            let fontTable = new SortedDictionary<int32, FontRep.FontChar> ()
            for c in font.chars do
                fontTable.[c.unicode] <- c
            let numChar: int32 = int32 fontTable.Count
            let part2Starts: int32 = 92
            let part3Starts = part2Starts + 10 * numChar
            let part4Starts = part3Starts + 8 * numChar
            let part5Starts = part4Starts
            let dontCare i = for i in [0..i-1] do writer.Write (byte 0)
            let writeHeader () =
                writer.Write numChar
                writer.Write numChar
                writer.Write part2Starts
                dontCare 8
                writer.Write numChar
                writer.Write numChar
                writer.Write part3Starts
                dontCare 12
                writer.Write (0: int32)
                writer.Write (0: int32)
                writer.Write part4Starts
                dontCare 12
                writer.Write part5Starts
                dontCare 4
                writer.Write width
                writer.Write height
                dontCare 8
            let writePart2 () =
                for fontChar in fontTable.Values do
                    writer.Write (byte 0)
                    writer.Write fontChar.width
                    writer.Write fontChar.height
                    writer.Write fontChar.xadvance
                    writer.Write fontChar.xoffset
                    writer.Write fontChar.yoffset
                    writer.Write fontChar.x
                    writer.Write fontChar.y
            let writePart3 () =
                let mutable i: int32 = 0
                for fontChar in fontTable.Values do
                    writer.Write fontChar.unicode
                    writer.Write i
                    i <- i + 1
            let writePart5 () =
                writer.Write ([|0x7a; 0x53; 0x30; 0x37|] |> Array.map byte)
            let writeBody () =
                writePart2()
                writePart3()
                writePart5()
            writeHeader ()
            writeBody ()
