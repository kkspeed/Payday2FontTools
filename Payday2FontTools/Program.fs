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

open System
open System.Drawing
open System.IO
open System.Windows.Forms

module Main =
    let MoveDdsToTexture file =
        let fileNameNoExt = Path.GetFileNameWithoutExtension file
        let dirname = Path.GetDirectoryName file
        let outDds = Path.Combine (dirname, fileNameNoExt + ".dds")
        let outTexture = Path.Combine (dirname, fileNameNoExt + ".texture")
        File.Copy (outDds, outTexture, true)
        File.Delete outDds
    let GetReader file =
        match Path.GetExtension file with
            | ".font" -> Some FontIO.DisselFont.ReadDisselFont
            | ".fnt" -> Some FontIO.BMFont.ReadBMFont
            | _ -> None
    let ReadFont file =
        match GetReader file with
            | Some reader  -> reader file
            | None -> failwith ("Unrecognized file name: " + file)
    type Window () as this =
        inherit Form ()
        let ButtonOk = new Button ()
        let ButtonClear = new Button ()
        let ButtonDelete = new Button ()
        let ButtonMoveUp = new Button ()
        let ButtonMoveDown = new Button ()
        let FontList = new ListView ()
        do
            this.Text <- "Payday2FontTools"
            this.Width <- 545
            this.Height <- 500
            this.FormBorderStyle <- FormBorderStyle.FixedDialog
            this.MaximizeBox <- false
            this.MinimizeBox <- false
            this.AllowDrop <- true
            this.DragEnter.AddHandler (fun s e -> this.Window_DragEnter s e)
            this.DragDrop.AddHandler (fun s e -> this.Window_DragDrop s e)
            FontList.Width <- 500
            FontList.Height <- 400
            FontList.Location <- new Point (15, 10)
            FontList.View <- View.List
            FontList.MultiSelect <- false
            ButtonOk.Text <- "Run"
            ButtonOk.Location <- new Point (15, 430)
            ButtonOk.Click.AddHandler (fun s e -> this.Window_Run ())
            ButtonClear.Text <- "Clear"
            ButtonClear.Location <- new Point (ButtonOk.Right + 10, 430)
            ButtonClear.Click.AddHandler (fun s e -> FontList.Clear ())
            ButtonDelete.Text <- "Delete"
            ButtonDelete.Location <- new Point (ButtonClear.Right + 10, 430)
            ButtonDelete.Click.AddHandler
                (fun _ _ -> for i in FontList.SelectedItems do i.Remove ())
            ButtonMoveUp.Text <- "Move Up"
            ButtonMoveUp.Location <- new Point (ButtonDelete.Right + 10, 430)
            ButtonMoveUp.Click.AddHandler
                (fun _ _ -> for i in FontList.SelectedItems do this.FontList_Move i -1)
            ButtonMoveDown.Text <- "Move Down"
            ButtonMoveDown.Location <- new Point (ButtonMoveUp.Right + 10, 430)
            ButtonMoveDown.Click.AddHandler
                (fun _ _ -> for i in FontList.SelectedItems do this.FontList_Move i 1)
            this.Controls.AddRange [| ButtonOk; ButtonClear; ButtonDelete;
                                ButtonMoveUp; ButtonMoveDown; FontList |]
        member this.Window_DragEnter sender (e: DragEventArgs) =
            if (e.Data.GetDataPresent DataFormats.FileDrop)
            then e.Effect <- DragDropEffects.Copy
        member this.Window_DragDrop sender (e: DragEventArgs) =
            let files = (e.Data.GetData DataFormats.FileDrop) :?> string array
            for file in files do
                match GetReader file with
                    | Some _ -> FontList.Items.Add file |> ignore
                    | None -> ()
        member this.FontList_Move item i =
            let newIndex = item.Index + i
            if newIndex >= 0
            then FontList.Items.RemoveAt item.Index
                 FontList.Items.Insert (newIndex, item) |> ignore
                 item.Selected <- true
        member this.Window_GetSaveFile () =
             let saveFileDialog = new SaveFileDialog()
             saveFileDialog.Filter <- "Dissel Font|*.font"
             saveFileDialog.Title <- "Save Font File"
             saveFileDialog.ShowDialog () |> ignore
             saveFileDialog.FileName
        member this.Window_Run () =
            let output = this.Window_GetSaveFile ()
            if FontList.Items.Count > 0 && output <> ""
            then
              this.Text <- "Payday2FontTools (Working...)"
              seq { for item in FontList.Items -> item.Text }
              |> Seq.map ReadFont
              |> Seq.reduce FontRep.(<->)
              |> FontIO.DisselFont.WriteDisselFont output
              MoveDdsToTexture output
              this.Text <- "Payday2FontTools (Done!)"

    [<STAThread>]
    do new Window () |> Application.Run
