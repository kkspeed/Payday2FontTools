Dim shell
Set shell = WScript.CreateObject ("WScript.shell")
Dim input
input = InputBox("Font type (large, medium or small): ")
shell.run ("Payday2FontComposer.exe " & input), 1, true
Dim fso
Set fso = WScript.CreateObject ("Scripting.FileSystemObject")
fso.CopyFile ("Output\\font_" & input & ".dds"), ("Output\\font_" & input & ".texture")
fso.DeleteFile ("Output\\font_" & input & ".dds")