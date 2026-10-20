# Namekeeper 0.4.6 networking notes

The direct authenticated map RPC and its administrator check from 0.4.5 are retained. The user's dedicated-server log confirmed an authorized request returning three positions. No new command or config setting is introduced.

Install 0.4.6 on the server and all clients. Patch-version strictness and the 12 synchronized gameplay settings remain unchanged.

Grass exclusion is local procedural presentation tied to each loaded world shop. Every client applies the same X/Z camp footprint and refreshes its clutter patches. It does not depend on player ownership, terrain paint replication, or world-file edits.

The two camp boars remain local animated displays. Shared-world-clock feeding and the cosmetic pet RPC are unchanged. Animal initialization no longer depends on optional food visuals, and missing animals retry individually without creating duplicate ZDOs or player ownership.

The common bowl and seven carrots contain no inventory, item drops, gameplay scripts or independent network state. The same fixed layout is reconstructed on each client. The dedicated server creates no local boar models, which is expected.

Existing shops, roof and door state, camp coordinates, terrain-leveling keys, prices, recipes and personalized item metadata are preserved. Existing worlds can be used to test this update. Server/client files are updated manually by the user.
