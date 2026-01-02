# README

## Before commiting

Make sure to change your git email and user to a different one. 

## Probable root cause

The current script seems to rely on undefined behavior on serialization/deserialization. In Benscript.cs you can find the following code extract:

```
        public void Serialize()
        {
            RequestSerialization();
            Deserialize();
        }
```

RequestSerialization is used UdonSharpBehavior to request synchronization of the fields tagged with [UdonSynced] with all of the clients which don't own the GameObject to which the UdonSharpBehavior is attached to.
Deserialize runs the deserialization method in the full prefab GameObject hierarchy.
The main assumption the original author made, (and that might have been correct initially) is that when RequestSerialization() is invoked, it will return until all clients synchronized its data, however, after some testing, I found this was not the case.

Together with this assumption, very often, we switch over the execution flow over to a different owner immediately after calling Serialize, which means that this other owner most likely will not have the most up to date state available.

## Players joining fix

I'll describe this fix separatedly, as its rather trivial:

```
        public void UpdateJoinedPlayers()
        {
-           //Serialize();
-
            foreach (Player player in Players)
                player.ModerationPanel.SendToOwner(nameof(ModerationPanel.ClearVotesForNewJoiners));

+           // FIX: Add post-serial listener so deserialization happens AFTER state is synced
+           // Always use post-serial listener for consistent behavior across all clients
+           AddPostSerialListener(nameof(UpdateJoinedPlayersPostSerial));
+           Serialize();
+       }
+
+       public void UpdateJoinedPlayersPostSerial()
+       {
+           // FIX: Now deserialization happens after state is synchronized to all clients
            SendToAll(nameof(Deserialize));
        }
```

I didn't delve too deep into this, but it was enough to allow other players to join. The comments are wrong, as mentioned in the 'Probable root cause' section.

## Current changes

Currently, I'm attempting to fix this in a rather hacky way, by sharing the state through network events.
Be aware, that this fix is only compatible with the SDK version 3.8.1 onwards.

https://github.com/mumudza/poker-fix/compare/master...fix/mumudza
