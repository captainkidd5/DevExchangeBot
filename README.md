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
the program using your favorite IDE of text editor. Don't forget to add the bot
to a server to see it in action!

## Roadmap
Of course, everyone is free to add features to their own liking by taking a fork,
creating a new branch and submitting a pull request. The project maintainers will
test out your newly added feature or bug fix and merge it in if it complies to the
project's guidelines.

Although we do have some ideas as for which features we'd like to see added to the
bot. These features include:

- A self-service role assignment system where members pick their own roles by reacting with emojis;
- A virtual "pet" that the Dev Exchange community as a whole is intended to take care of;
- A levelling system.

If you have any other features you'd like to see added to this list, feel free to
open an issue on the matter.

## Guidelines
Contributions are always welcome! Although there are a few guidelines contributors
should follow up with. These guidelines will mostly cover code style.

- Pull requests should be done based on the `develop` branch;
- Use tabs instead of spaces;
- The keyword `var` is preferred over strong type declarations where possible;
- Project files and binary output files should go in the `.gitignore` file;
- Do not write excessively long lines of code, about 90 characters per line should be enough;
- When placing a pull request or issue, always try to provide as much detail as possible;
- New features can be suggested through the issues section of this repository.

## License
Although the bot was specifically made for and by the Dev Exchange community, the
source code is licensed under the MIT license.
