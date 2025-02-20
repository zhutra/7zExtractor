# 7zExtractor

📂 A simple command-line utility that selectively extracts specific folders from archive files supported by 7-Zip. If multiple directories are present in a file, this tool attempts to assign a _root_ directory to one of the possible folder matches specified.

## Usage

```
7zExtract.exe <archive_file> --to <destination_folder> [--re "new_name"] [--from "folder1" or "folder2" or ...]
```
```
  <archive_file>                        Required. Path to file
  --to <destination_folder>             Required. If it doesn't exist, it will be created
  --re "new_name"                       Optional. Rename the extracted folder with
  --from "..." or ...                   Optional. Match a folder to extract from
```

## Building

```
dotnet build -c Release
```

Requirements:
- [.NET Core SDK 6.0](https://dotnet.microsoft.com/download/dotnet-core/6.0)
- [7z.dll](https://sourceforge.net/p/sevenzip/discussion/45798/thread/f4e969b197/#fb92) - add to project's directory

## License

The license for this project is available at [7-zip.org/license.txt](https://7-zip.org/license.txt).

## Credits

This program makes use of the **7-Zip** library for archive extraction.  Thank you to Igor Pavlov from the [7-Zip](https://sourceforge.net/projects/sevenzip/) project, as well as Alexander Selishchev from the [SevenZipExtractor](https://github.com/adoconnection/SevenZipExtractor) repository.
