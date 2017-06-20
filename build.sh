if [ -z "$build_config" ]; then export build_config="Release"; fi #default is Release

msbuild /p:Configuration="$build_config" Computator.NET.Charting/Computator.NET.Charting.csproj