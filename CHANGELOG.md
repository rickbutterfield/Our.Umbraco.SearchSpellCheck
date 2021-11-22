# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/) and this project adheres to [Semantic Versioning](https://semver.org/).

## [1.0.0-beta.6] - 2021-11-22
### Added
- Added `nodeName` to index by default so that the Examine Dashboard shows the node name

### Changed
- Adjusted reference to Umbraco temp folder for v9
- Updated index settings for v8

## [1.0.0-beta.5] - 2021-11-20
### Changed
- Downgraded `Lucene.Net` dependency to `4.8.0-beta00014` for Umbraco 9 compatibility

## [1.0.0-beta.4] - 2021-11-19
### Added
- Option to enable or disable index on startup (default enabled)
- Option to enable or disable indexing on content published (default enabled)

## [1.0.0-beta.3] - 2021-11-19
### Removed
- Hotfix - disable packaging files into the NuGet package

## [1.0.0-beta.2] - 2021-11-18
### Changed
- Hotfix for getting the correct local TEMP path when using `"LocalTempStorageLocation": "EnvironmentTemp"`

## [1.0.0-beta.1] - 2021-11-18
### Added
- Multi-targeted package for both .NET Framework and .NET Core
- Added support for multi-word search
- Added options for configuring background rebuild (toggling on and off, and determining the time between starting and repeating)

## [1.0.0-alpha] - 2021-08-13
### Added
- Configure the index name via web.config
- Configure the indexed field(s) via web.config

## [0.0.1] - 2021-08-11
### Added
- Initial release! Just the basic source code for now.

[1.0.0-beta.4]: https://github.com/rickbutterfield/Our.Umbraco.SearchSpellCheck/releases/tag/release-1.0.0-beta.4
[1.0.0-beta.3]: https://github.com/rickbutterfield/Our.Umbraco.SearchSpellCheck/releases/tag/release-1.0.0-beta.3
[1.0.0-beta.1]: https://github.com/rickbutterfield/Our.Umbraco.SearchSpellCheck/releases/tag/release-1.0.0-beta.1
[1.0.0-alpha.2]: https://github.com/rickbutterfield/Our.Umbraco.SearchSpellCheck/releases/tag/release-1.0.0-alpha.2
[1.0.0-alpha]: https://github.com/rickbutterfield/Our.Umbraco.SearchSpellCheck/releases/tag/release-1.0.0-alpha
