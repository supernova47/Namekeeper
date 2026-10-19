# Namekeeper 0.3.10 test plan

Use a disposable test world and character. Do not test on the only copy of an important world.

## Versions and connections

- [ ] Install Namekeeper 0.3.10, BepInExPack Valheim, and Jötunn on the dedicated server and client. Confirm that the server starts without errors and registers the shop and roof.
- [ ] Connect a client running Namekeeper 0.3.10 to a server running 0.3.10; the connection should succeed.
- [ ] Connect a client running 0.3.8 to a server running 0.3.10; Jötunn should reject the version mismatch.
- [ ] Connect a client running Namekeeper 0.3.10 to a server running 0.3.8; the 0.3.8 server should also reject the mismatch.
- [ ] Record the known limitation: a 0.3.7 server can accept clients running 0.3.8 or 0.3.10 because version enforcement was not present in 0.3.7. Mismatches should be rejected after the server is updated to 0.3.8 or newer.

## Recipe and materials

- [ ] Open the Hammer menu after server settings synchronize. Confirm the shop recipe shows the correct Wood, Fine Wood, Round Log, Stone, and Coin icons rather than repeated shield icons.
- [ ] Change one or more material amounts on the server, connect a client with different local values, and confirm the server amounts appear with the correct icons.
- [ ] Check the separate roof recipe: four Wood, the correct icon, and normal placement.
- [ ] Collect each recipe material for the first time and confirm the shop unlocks with Valheim's standard notification.

## Multiplayer gameplay

- [ ] Confirm the server synchronizes service prices, shop materials, merchant arrival delay, roof requirement, and text length limits.
- [ ] Build the shop and roof. Check merchant arrival, door interaction, and persistence after restarting the world.
- [ ] Change an item's name and description. Check payment, item transfer, and persistence of custom text.
- [ ] Check local language, dialogue, voice volume, and effects; these settings must not change another player's settings.
- [ ] Confirm the server log has no GUI, rendering, audio, or Player.m_localPlayer errors.
