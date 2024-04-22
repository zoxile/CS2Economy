# CS2Economy
A economy plugin for CS2 with several features. It's my first big plugin, might be some inefficient parts but I am happy to learn! Thanks to darrenrid for the inspiration!

## Requirements
[MultiAddonManager by xen-000](https://github.com/Source2ZE/MultiAddonManager) (For Models if you don't put it in the map)

[ResourcePrecacher by KillStr3aK](https://github.com/KillStr3aK/ResourcePrecacher) (For Models)

[CounterStrikeSharp by roflmuffin](https://github.com/roflmuffin/CounterStrikeSharp) (API)

Big thanks to all these developers!

## Features
- Economy system (Credits)
- Gambling games (Roulette and more to come)
- Shop for models
- Removing models and agents temporarily

## TODO
- Shop for items (weapons, grenades, configurable things)
- Gifting items with credits
- Gift sounds
- LANG folder
- Make roulette bet amount's configurable
- Expand the config for everything in the plugin

## Configuration
After launching the plugin for the first time, a config file will be created in counterstrikesharp's config directory, there you will need to enter necessary info about how you would like the plugin to function, database credentials but also the module directory.
That way another .json will be created in the module directory called models.json. There you can add the necessary info about the models, and if you have succesfully precached them, the models is going to be added into the store.

## Commands
- css_balance | (Shows the balance of the player only to him)
- css_flex | (Shows the balance of the player to everyone)
- css_gift <player> <amount> | (Gifts the target player a specified amount of credits)
- css_pay <player> <amount> | (Pays the target player a specified amount of credits) //Same as gift
- css_givecredits <player> <amount> | (Admin command to give players credits) [RequiresPermissions("@css/cheats")]
- css_secretcredit <player> <amount> | (Admin command to give players credits but in secret) [RequiresPermissions("@css/cheats")]
- css_removecredits <player> <amount> | (Admin command to remove players credits) [RequiresPermissions("@css/cheats")]
- css_removecreditsoffline <steamid> <amount> | (Admin command to remove players credits through steamid) [RequiresPermissions("@css/cheats")]
- css_givecreditsoffline <steamid> <amount> | (Admin command to remove players credits through steamid) [RequiresPermissions("@css/cheats")]
- css_roulette <amount> | (Opens up a menu for a roulette game where you can choose between black, green and red!)
- css_roulettetoggle | (Toggles roulette on and off) [RequiresPermissions("@css/rcon")]
- css_removemodels | (Disables models for everyone temporarily for a round. This works also for CS2's own agents and sets everyones models to default.) [RequiresPermissions("@css/changemap")]
