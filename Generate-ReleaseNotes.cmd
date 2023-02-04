rem https://github.com/StefH/GitHubReleaseNotes

SET version=1.1.2

GitHubReleaseNotes --output CHANGELOG.md --skip-empty-releases --exclude-labels question invalid doc duplicate --version %version% --token %GH_TOKEN%