Dim shell
Set shell = WScript.CreateObject ("WScript.shell")
Dim input, outputFile, bmInput, stockInput
input = InputBox("Font type (large, medium or small): ")
outputFile = "Output/font_" & input & ".font"
bmInput = "BMOutput/font_" & input & ".dds.fnt"
stockInput = "StockFont/font_" & input & ".font"
shell.run ("Payday2FontTools.exe " & outputFile & " -f " & stockInput & " " & bmInput), 1, true
