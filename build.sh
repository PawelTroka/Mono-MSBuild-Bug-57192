if [ -z "$build_config" ]; then export build_config="Release"; fi #default is Release

msbuild /p:Configuration="$build_config" Mono-MSBuild-Bug-57192/Mono-MSBuild-Bug-57192.csproj