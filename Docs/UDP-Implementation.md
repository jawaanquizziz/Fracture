# VALOMON LAN protocol

The campaign uses `LanUdpTransport` and `CampaignNetwork`; the older TCP prototype is not instantiated by `VALMON_Campaign.unity`.

- A host binds an IPv4 UDP socket, normally port 7788. A joiner binds an ephemeral UDP port and contacts the host by IPv4 and room code.
- A versioned handshake checks the catalog/rule signature before admitting the other endpoint. Subsequent traffic is bound to that endpoint and match session. Exactly two players occupy a room.
- Reliable controls carry ordered sequence numbers. Complete messages are acknowledged, duplicates are acknowledged without being executed again, and missing messages are retried every 180 milliseconds. Out-of-order controls wait for the missing earlier message.
- State and movement use a separate sequenced lane. Old complete snapshots are discarded. A newer full snapshot repairs missed visual/state updates.
- Messages are split into 1,100-byte payloads with an 18-byte header. Incomplete assemblies expire, and queue, message-size and sequence-window limits bound resource use.
- The host advances the timer, moves the Tamer controller, validates capture range and corruption energy, resolves attacks/PP/status, removes defeated teammates and decides level/final outcomes. The client sends input/actions and renders authoritative snapshots.
- Battle actions include stage, screen, battle and turn identifiers. Stale actions, wrong-role actions and out-of-turn actions are rejected. Client commands are throttled, and one battle decision can be submitted per observed turn.
- Map seed, spawn state, both selected characters, teams, PP/HP/status, capture progress, corruption energy, battle turn, pause, timer, quota and result are replicated. AI opponents run only in solo mode.
- Both players share pause. A lost connection ends the current session; it does not silently introduce AI or advance an independent client timer.

The editor validator performs UDP loopback loss/retry/ordering/fragment tests. Development builds additionally support an explicit two-process campaign test with `-valomonNetSmoke host|client`. Tests force selected terminal battle states; ordinary battle math is tested by the separate 120 seeded simulations.

The final portable player is built without development test hooks. Physical two-laptop connectivity still depends on the selected LAN/hotspot and Windows Firewall settings; see `Multiplayer-Setup.md`.
