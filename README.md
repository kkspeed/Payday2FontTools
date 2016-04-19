# Payday2FontTools

This tool helps creating Payday 2 font files. It's heavily based on
the tutorial written by (MaxShouldier)[http://forums.lastbullet.net/showthread.php?tid=1285&pid=5489#pid5489].

The tool depends on BMFont, which is released under Zlib license
(compatible with GPL v3 license that this tool is released under).

This repo is only for advanced users, i.e. if you want to compile this
tool from source. Otherwise, please refer to the download branch to
get latest binary release.

## Building Payday2FontTools
Payday2FontTools can be built with Mono or .NET, on Windows, Linux or
OSX. For the sake of simplicity, I'll only cover Windows build. Users
familiar with this process should transit to other platform without
any difficulty. However, since the binary included in MagickNET is
compiled specifically for Windows, at the moment, this tool does not
work with other OS (Sorry!).

You'll need F# and MSBuild to compile this project. I'd recommend you
get Visual Studio 2015 Community. After installation, just go to the
directory of the project and type:

    .paket/paket.bootstrapper.exe
    .paket/paket.exe install
    packages/FAKE/tools/FAKE.exe build.fsx

If all goes well, you should see a build directory and inside it are
all the executables, libraries etc you'll find for this project.
