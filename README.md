# SponsorBoi [![Build Status](https://jenkins.karlofduty.com/job/CI/job/SponsorBoi/job/master/badge/icon)](https://jenkins.karlofduty.com/blue/organizations/jenkins/CI%2FSponsorBoi/activity) <!--[![Downloads](https://img.shields.io/github/downloads/KarlOfDuty/SponsorBoi/total.svg)](https://github.com/KarlOfDuty/SponsorBoi/releases)--> [![Release](https://img.shields.io/github/release/KarlofDuty/SponsorBoi.svg)](https://github.com/KarlOfDuty/SponsorBoi/releases) [![Discord Server](https://img.shields.io/discord/430468637183442945.svg?label=discord)](https://discord.gg/C5qMvkj)
A Discord bot which syncs Github Sponsors to Discord.

Discord users may register their Github accounts using bot commands and they can be granted roles depending on their sponsorship tiers via the Github API.

The bot works by having the user create an issue in one of your repositories containing their Discord ID, then run the link command in your Discord server. The bot then checks the most recent issues of the repo for the ID and links the Github user with the Discord user when it is found.

The user is then granted a role according to your config and the bot will re-check all linked users every two hours (by default) to make sure they are still a sponsor. If a user unlinks themselves they also lose their sponsor role until they re-link. If a linked user re-joins the Discord server they will retain their role.

## Commands

| Command | Description |
|--- |---- |
| `/link` | Links your Discord account to your Github account. |
| `/adminlink <discord user> <github user>` | Manually links a Discord account to a Github account. |
| `/unlink` | Unlinks your Github account. |
| `/adminunlink <discord user>` | Unlinks a user's Github account. |
| `/recheck` | Forces a recheck of all linked users' sponsor status. |
| `/recheck <discord user>` | Forces a recheck of a specific user's sponsor status. |

## Setup

1. Set up a mysql server, create a user and empty database for the bot to use.

2. [Create a new bot application](https://discordpy.readthedocs.io/en/latest/discord.html).

3. Download the bot for your operating system, either a [release version](https://github.com/KarlOfDuty/SponsorBoi/releases) or a [dev build](https://jenkins.karlofduty.com/blue/organizations/jenkins/SponsorBoi/activity).

4. Run `./SponsorBoi` on Linux or `./SponsorBoi.exe` on Windows, the config is created for you.

5. Set up the config (`config.yml`) to your specifications, there are instructions inside and also further down on this page. If you need more help either contact me in Discord or through an issue here.

## Config:
```yaml
github:
    # Github personal access token - https://github.com/settings/tokens - Requires 'read:org' permission (and maybe 'read:user'?) to fetch sponsors.
    token: "<add-token-here>"

    # Amount of time in minutes between re-checking users with registered roles, recommended to keep high. Set to 0 to disable alltogether.
    auto-prune-time: 120

    sync:
        # The name of the repository to check for verification requests, must be owned by the same user as the token above.
        repository-name: "MyRepo"

        # The owner of the repository, most likely your github username
        owner-name: "MyUsername"

        # The issue title (all characters must be allowed in a URL)
        issue-title: "Automated Discord Link"

        # Optional: Set a label (all characters must be allowed in a URL)
        issue-label: ""

bot:
    # Bot token.
    token: "<add-token-here>"

    # Decides which messages are shown in console
    # Possible values are: Critical, Error, Warning, Information, Debug.
    console-log-level: "Information"

    # A list of the dollar amount of the sponsorship tiers paired with corresponding role ids.
    roles:
        - "10": "000000000000000000"
        - "5":  "111111111111111111"
        - "1":  "222222222222222222"

    # Sets the type of activity for the bot to display in its presence status.
    # Possible values are: Playing, Streaming, ListeningTo, Watching, Competing.
    presence-type: "Watching"

    # Sets the activity text shown in the bot's status.
    presence-text: "for new sponsors"

    # Your Discord server's ID
    server-id: 000000000000000000

database:
    # Address and port of the mysql server.
    address: "127.0.0.1"
    port: "3306"
    # Name of the database to use.
    name: "sponsorboi"
    # Username and password for authentication.
    user: "username"
    password: "password"
```
