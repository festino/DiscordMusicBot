# DiscordMusicBot
## Overview
A bot for Discord that allows you to play audio from Youtube in a voice channel using commands in a text channel.

## Key Features
* Commands: help, play, stop, skip, undo, now, list (prefix can be configured in config.yml)
* The last message in the “native” text channel contains information about the current video
* Bot messages are configured in lang.yml
* Support any Youtube videos (including shorts)
* Support multiple servers
* Volume leveling (still in progress)

 ## Installation
Build the project and executables/dlls yt-dlp, ffmpeg, opus, libsodium.  
Or download [the latest release](https://github.com/festino/DiscordMusicBot/releases) and ready-made executable files (will be available soon).

## Usage
You need to [create a Discord bot](https://discord.com/developers/applications?new_application=true), invite it to the desired server (only the owner can add an unverified bot). Example invite link: https://discord.com/api/oauth2/authorize?client_id={BOT_ID}&permissions=274881120256&scope=bot (replace "{BOT_ID}")  
In credentials.yml, insert the bot's API key, as well as [Youtube API public key](https://developers.google.com/youtube/v3/docs) for now.  
Run the project.

## Roadmap
* Improve the volume leveling
* “meme” command, which temporarily interrupts the current video and plays another
* Improve the appearance of bot messages
* Do not require “play” command in “native” text channel 
* Full persistence to reboots
* Update yt-dlp automatically
* Support for broadcasts, clips, timecodes and playlist indices
* Use SponsorBlock to skip segments
