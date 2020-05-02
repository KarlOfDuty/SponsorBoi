# SponsorBoi
A Discord bot which syncs Github Sponsors to Discord.

Discord users may register their Github accounts using bot commands and they can be granted roles depending on their sponsorship tiers via the Github API.

## Config:
```yaml
github:
    # Github personal access token - https://github.com/settings/tokens - Requires user read permissions to fetch sponsors.
    token: "<add-token-here>"
    # Amount of time in minutes between each refresh, it is recommended to keep this above 5 minutes.
    update-rate: "15"

bot:
    # Bot token.
    token: "<add-token-here>"
    # Decides what messages are shown in console, possible values are: Critical, Error, Warning, Info, Debug.
    console-log-level: "Info"
    # A list of the dollar amount of the sponsorship tiers paired with corresponding role ids.
    roles:
      - "10": "111111111111111111"
      - "5":  "222222222222222222"
      - "1":  "333333333333333333"

database:
    # Address and port of the mysql server
    address: "127.0.0.1"
    port: "3306"
    # Name of the database to use
    name: "sponsorboi"
    # Username and password for authentication
    user: "username"
    password: "password"
```