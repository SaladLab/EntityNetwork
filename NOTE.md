# TODO

## TrackableData Extensibility

- Current implementation supports only TrackablePoco.
  This limitation is based on code generation using syntax analysis.
  This can be alleviated by heuristic type guess.

## Time

- Time is an essential factor for game

## Ownership

Every entity has ownership.
- If owner leave the network, owned entity will be despawned.
- Only server and owner client can control server entity.
- Ownership can be handed over to another client
  - When Owner leave but entity should stay.
  - When Owner hand over voluntarily

## State

- Use TrackableData! (Done)
- Client-side update ? (allowed?)

## Constructor or Initializer *Done*

- It's required to init entities showing up properly. (Done with Snapshot)

## Saved Method

- [Saved] for new comers (REPLACED WITH TrackableData)

# IDEA

## Intrusive Client in Server *Done*

I am not sure but it is a considerable idea that ServerEntity may inherit ClientRef
to allow use methods of ClientRef in a natual fashion.

## Fast redirection

For fast forwarding, it's better to have an option for redirecting message toward self.
When a client send 'Rpc' to master, master may broadcast this message to all except sender
and sender client get Rpc by itself.

## Inheritance

To support IMonster : ICreature
