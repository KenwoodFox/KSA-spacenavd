# KSA-Spacenavd

[SpaceMouse](https://3dconnexion.com/us/spacemouse/) input for [Kitten Space Agency](https://ahwoo.com/app/100000/kitten-space-agency)

## Build

Requires the .NET 10 SDK, `curl`, and `unzip`. 
I assume you have the KSA.dll at  `/opt/kittenspaceagency/KSA.dll` but if you dont, 
use the override with `-p:KsaDll=/path/to/KSA.dll`.

I didn't want to grab the starmap API manually every time so if you dont have it, building should
pull `StarMap.API.dll` into `KSA-Spacenavd/deps/`.

```
dotnet build -c Release
```

Output is `KSA-Spacenavd/bin/Release/net10.0/`. Copy `KSA-Spacenavd.dll`, `KSA-Spacenavd.deps.json`, and `mod.toml` into a mod folder under the KSA content directory, and enable it in `manifest.toml`:

```
[[mods]]
id = "KSA-Spacenavd"
enabled = true
```

## Oneliner

I don't neccicarily think you should do this but, i did copy/paste a big oneliner for
everything. Just for testing.

```shell
dotnet build -c Release && mkdir -p "$HOME/Documents/My Games/Kitten Space Agency/mods/KSA-Spacenavd" && cp KSA-Spacenavd/bin/Release/net10.0/{KSA-Spacenavd.dll,KSA-Spacenavd.deps.json,mod.toml} "$HOME/Documents/My Games/Kitten Space Agency/mods/KSA-Spacenavd/" && sed -i '/id = "KSA-Spacenavd"/q; $a [[mods]]\nid = "KSA-Spacenavd"\nenabled = true' "$HOME/Documents/My Games/Kitten Space Agency/manifest.toml" && for d in "$HOME/.local/share/Borea/Instances/"*/mods/KSA-Spacenavd; do [ -d "$d" ] && cp KSA-Spacenavd/bin/Release/net10.0/{KSA-Spacenavd.dll,KSA-Spacenavd.deps.json,mod.toml} "$d/"; done
```