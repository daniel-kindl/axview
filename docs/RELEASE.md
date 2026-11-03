# Versions and releases

AxView uses [MinVer](https://github.com/adamralph/minver).
MinVer gives the version from the latest git tag in the history of HEAD.
The version obeys [SemVer 2.0](https://semver.org).

## Tags

Put `v` before the version. Use an annotated tag.

| Tag | Use |
|---|---|
| `v1.3.0-beta.1` | A feature build for a test. |
| `v1.3.0-rc.1` | A release candidate. |
| `v1.3.0` | A release. |
| `v1.2.1` | A hotfix of `v1.2.0`. |

- Put a dot before the number of a pre-release: `rc.10` comes after `rc.2`. `rc10` comes before `rc2`.
- Pre-release words sort in alphabetical order: `alpha` < `beta` < `rc`. A release comes after all its pre-releases.
- Do not move or delete a tag after you push it.

## Versions between tags

| HEAD | Version |
|---|---|
| On the tag `v1.2.0` | `1.2.0` |
| 3 commits after `v1.2.0` | `1.2.1-alpha.0.3` |
| 2 commits after `v1.3.0-rc.1` | `1.3.0-rc.1.2` |
| No tag in the history | `0.0.0-alpha.0.<commit count>` |
| No git work tree, for example the source archive | `0.0.0-source` |

The About dialog and the name of the binary archive show this version.

## Make a release

1. Make sure that `dotnet test` passes on HEAD.
2. Tag HEAD: `git tag -a v1.3.0 -m "AxView 1.3.0"`.
3. Push the tag: `git push origin v1.3.0`.
4. Write the archives: `./pack.ps1 all`.

## Make a hotfix

1. Make a branch from the release tag: `git switch -c hotfix/1.2.1 v1.2.0`.
2. Fix the problem, and commit.
3. Tag the fix `v1.2.1`, and make the release.
4. Merge the hotfix branch into `main`.

## Clone for a build

MinVer needs the full history and the tags.
A shallow clone gives `0.0.0-alpha.0.1`. In CI, fetch all history, for example `fetch-depth: 0`.
