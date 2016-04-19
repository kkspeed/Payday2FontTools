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

open System
open System.Collections.Generic
open System.IO
open System.Text
open FSharp.Data

let mutable preserveOriginal = false

type Header =
    { numChars : int32;
      part2Offset : int32;
      part3Offset : int32;
      part4Offset : int32;
      part5Offset : int32;
      numKerningPairs : int32;
      textureWidth : int32;
      textureHeight : int32 }
    override m.ToString () =
        sprintf "textureWidth=%d, textureHeight=%d\n"
            m.textureWidth m.textureHeight

type FontProperty = {
    width : uint8;
    height : uint8;
    xadvance : int8;
    xoffset : int8;
    yoffset : int8;
    x : uint16;
    y : uint16
}

type Sprite = {
    unicode : int32;
    string : string;
    tileIndex : int32;
}

type Char = {
    sprite : Sprite;
    property : FontProperty
}

let ignoreBytes (reader: BinaryReader) num = reader.ReadBytes num |> ignore

let readHeader (reader: BinaryReader) =
    let numChars = reader.ReadInt32 ()
    ignoreBytes reader 4
    let part2Offset = reader.ReadInt32 ()
    ignoreBytes reader 8  // Ignore the non-sense region.
    ignoreBytes reader 8  // Ignore the redundant # of characters.
    let part3Offset = reader.ReadInt32 ()
    ignoreBytes reader 12
    let numKerningPairs = reader.ReadInt32 ()
    ignoreBytes reader 4
    let part4Offset = reader.ReadInt32 ()
    ignoreBytes reader 12
    let part5Offset = reader.ReadInt32 ()
    ignoreBytes reader 4
    let textureWidth = reader.ReadInt32 ()
    let textureHeight = reader.ReadInt32 ()
    ignoreBytes reader 8
    { numChars = numChars;
      part2Offset = part2Offset;
      part3Offset = part3Offset;
      part4Offset = part4Offset;
      part5Offset = part5Offset;
      numKerningPairs = numKerningPairs;
      textureWidth = textureWidth;
      textureHeight = textureHeight; }

let readPart2 (reader : BinaryReader) num offset =
    reader.BaseStream.Seek (offset, SeekOrigin.Begin) |> ignore
    [ for i in 1 .. num ->
        ignoreBytes reader 1
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

let readPart3 (reader : BinaryReader) num offset =
    reader.BaseStream.Seek (offset, SeekOrigin.Begin) |> ignore
    [ for i in 1 .. num ->
        let unicode: int32 = reader.ReadInt32 ()
        let character = Convert.ToChar unicode
        let tileIndex = reader.ReadInt32 ()
        { unicode = unicode
        ; string = character.ToString ()
        ; tileIndex = tileIndex } ]

let readFontFile (file: string) =
    let stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)
    let reader = new BinaryReader (stream)
    let header = readHeader reader
    let part2 = readPart2 reader header.numChars (int64 header.part2Offset)
    let part3 = readPart3 reader header.numChars (int64 header.part3Offset)
    let fontTable = new SortedDictionary<int32, Sprite * FontProperty> ()
    for (property, sprite) in List.zip part2 part3 do
        fontTable.Add (sprite.unicode, (sprite, property))
    fontTable

type BmFont = XmlProvider<"""<?xml version="1.0"?>
<font>
  <info face="WenQuanYi Zen Hei" size="-15" bold="0" italic="0" charset="" unicode="1" stretchH="100" smooth="1" aa="1" padding="0,0,0,0" spacing="1,1" outline="0"/>
  <common lineHeight="18" base="14" scaleW="2048" scaleH="2048" pages="1" packed="0" alphaChnl="0" redChnl="4" greenChnl="4" blueChnl="4"/>
  <pages>
    <page id="0" file="output_test.dds_0.dds" />
  </pages>
  <chars count="223">
    <char id="0" x="1686" y="0" width="1" height="1" xoffset="0" yoffset="0" xadvance="0" page="0" chnl="15" />
    <char id="1" x="1736" y="0" width="1" height="1" xoffset="0" yoffset="0" xadvance="0" page="0" chnl="15" />
    <char id="2" x="1724" y="0" width="1" height="1" xoffset="0" yoffset="0" xadvance="0" page="0" chnl="15" />
    <char id="3" x="1734" y="0" width="1" height="1" xoffset="0" yoffset="0" xadvance="0" page="0" chnl="15" />
  </chars>
</font>""">

let readBmFont (file:string) (fontTable: SortedDictionary<int32, Sprite * FontProperty>) offset =
    let font = BmFont.Load file
    for c in font.Chars.Chars do
        let value = ({unicode = c.Id; string = ""; tileIndex = 0;},
                      { width = uint8 c.Width
                      ; height = uint8 c.Height
                      ; xadvance = int8 c.Xadvance
                      ; xoffset = int8 c.Xoffset
                      ; yoffset = int8 (c.Yoffset - 1)
                      ; x = uint16 (c.X + offset)
                      ; y = uint16 c.Y })
        if preserveOriginal
        then if fontTable.ContainsKey c.Id |> not
             then fontTable.[c.Id] <- value
        else fontTable.[c.Id] <- value
    fontTable

let combineImage (original: string) (newImage: string) (output: string) =
    use images = new ImageMagick.MagickImageCollection()
    let img1 = new ImageMagick.MagickImage (original)
    let img2 = new ImageMagick.MagickImage (newImage)
    images.Add img1
    images.Add img2
    use result = images.AppendHorizontally ()
    result.Write output
    (img1.Width, img1.Width + img2.Width, max img1.Height img2.Height)

let writeBinaryFont (output: string) (fontTable: SortedDictionary<int32, Sprite * FontProperty>) (newWidth: int32) (newHeight: int32) =
    use stream = new FileStream (output, FileMode.Create, FileAccess.Write)
    use writer = new BinaryWriter (stream)
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
        writer.Write newWidth
        writer.Write newHeight
        dontCare 8
    let writePart2 () =
        for (sprite, fontP) in fontTable.Values do
            writer.Write (byte 0)
            writer.Write fontP.width
            writer.Write fontP.height
            writer.Write fontP.xadvance
            writer.Write fontP.xoffset
            writer.Write fontP.yoffset
            writer.Write fontP.x
            writer.Write fontP.y
    let writePart3 () =
        let mutable i: int32 = 0
        for (sprite, fontP) in fontTable.Values do
            writer.Write sprite.unicode
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

[<EntryPoint>]
let main argv =
    let fontType = argv.[0]
    preserveOriginal <- Array.exists (fun x -> "--preserve".Equals x) argv
    let image1 = @"StockFont\font_" + fontType + ".dds"
    let image2 = @"BMOutput\font_" + fontType + ".dds_0.dds"
    let imageOut = @"Output\font_" + fontType + ".dds"
    let (xoffset, newWidth, newHeight) = combineImage image1 image2 imageOut
    let fontTable = readFontFile (@"StockFont\font_" + fontType + ".font")
    let newFontTable = readBmFont (@"BMOutput\font_" + fontType + ".dds.fnt") fontTable xoffset
    writeBinaryFont (@"Output\font_" + fontType + ".font") newFontTable newWidth newHeight
    0
