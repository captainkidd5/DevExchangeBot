# Dev Exchange Bot
Welcome to the Dev Exchange Bot project! The goal of this project is to provide a
community-made Discord bot to the Dev Exchange Discord server.

To get you up to speed with the project, please read the rest of the README below!

## Getting Started
First thing first, create a new Discord application and copy its token over. After
cloning the project, copy the `config.example.json` file in the root of the project
and name it `config.json`.

Paste the bot token of the newly created Discord application in the `Token` field
of the configuration file. Take the bot for a test drive by compiling and running
the program using your favorite IDE of text editor.

Now, open up a terminal and run the following commands to pull in the latest
DSharpPlus NuGet sources and restore the project's dependencies:
```
dotnet nuget add source https://nuget.emzi0767.com/api/v3/index.json
dotnet restore
```
Don't forget to enable all intents on the Discord configuration page and add
the bot to a server to see it in action!

## Roadmap
Most initial features have been added at this moment. But if you want to add something
new or you want to introduce a bug fix, feel free to take a fork of the repository
and open an issue on what you would like to change. A maintainer will then have a look
at your change and test it out!

## Guidelines
Contributions are always welcome! Although there are a few guidelines contributors
should follow up with. These guidelines will mostly cover code style.

- Pull requests should be done based on the `develop` branch;
- It is recommended to work on a separate branch on your own fork of the project, e.g. `patch-1`, `patch-2`, etc.
- Use tabs instead of spaces;
- The keyword `var` is preferred over strong type declarations where possible;
- Try to comply to already established systems instead of writing your own next to it;
- Avoid underscores in local variables unless it's a class field;
- Avoid abbreviated variable names (e.g. use `builder` instead of `sb` and `message` instead of `msg`);
- Prefer ID's to be formatted as `Id` instead of `ID` (e.g. use `messageId` instead of `messageID`);
- Project files and binary output files should go in the `.gitignore` file;
- Do not write excessively long lines of code, about 90 characters per line should be enough;
- When placing a pull request or issue, always try to provide as much detail as possible;
- New features can be suggested through the issues section of this repository.

## Credits

- [JustAeris](https://github.com/JustAeris) - Development of the storage system, level system, heart board and quoting system;
- [CoolGabrijel](https://github.com/CoolGabrijel) - Development of the role menu feature;
- Rafe (link coming soon!) - Design and rendering of the Alt + F4 background of the bot's avatar.

## License
Although the bot was specifically made for and by the Dev Exchange community, the
source code is licensed under the MIT license.
