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

module FontRep =
    type FontChar = {
          unicode : int32
        ; width : uint8
        ; height : uint8
        ; xadvance : int8
        ; xoffset : int8
        ; yoffset : int8
        ; x : uint16
        ; y : uint16
    }

    type Font = {
        image : ImageMagick.MagickImage
        ; chars : FontChar seq
    }

    let (<->) f1 f2 =
        let images = new ImageMagick.MagickImageCollection ()
        images.Add f1.image
        images.Add f2.image
        let result = images.AppendHorizontally ()
        { image = result
        ; chars = f2.chars
          |> Seq.map (fun f -> {f with x = f.x + uint16 f1.image.Width})
          |> Seq.append f1.chars
        }
