# Bundled animal voice recordings

The user supplied four MP3 files and their Pixabay source URLs for integration into this mod.
Only processed clips used by the game are included as build assets. The original uploaded MP3s are not committed or distributed.
These recordings are third-party content, not original project artwork and not CC0 assets.

| Animal | Attribution from supplied filename | Source | Processed duration |
| --- | --- | --- | --- |
| Sheep | Universfield | https://pixabay.com/es/sound-effects/naturaleza-sheep-bleat-122256/ | 1.543 s |
| Cat | magiaz | https://pixabay.com/es/sound-effects/naturaleza-gato-462790/ | 3.000 s |
| Dog | Dragon Studio | https://pixabay.com/es/sound-effects/naturaleza-free-dog-bark-419014/ | 1.000 s |
| Horse | Dragon Studio | https://pixabay.com/es/sound-effects/naturaleza-horse-neigh-515279/ | 2.492 s |

## Source terms

Source terms: [Pixabay Content License summary](https://pixabay.com/service/license-summary/) and [full terms](https://pixabay.com/service/terms/).
The clips are integrated components of the mod, not a standalone audio download pack. Do not resell or redistribute them as standalone sound assets or treat the project code's terms as a new license for them.
Direct retrieval of the source pages/license text was blocked by this environment's HTTP proxy (403); the links and uploader-provided file identities are recorded, without claiming a fresh online rights verification.
`assets/audio/SOURCES.json` records original/processed SHA256 hashes, source links, processing and exact durations.

## Preparation and playback

- Leading silence removed; clips trimmed to at most 3 seconds.
- Loudness normalization target -18 LUFS / -2 dBTP, plus short entry/exit fades.
- Mono, 44100 Hz, signed PCM16 WAV. The strict runtime decoder rejects oversized, invalid or over-three-second clips.
- The four processed WAVs are embedded in `Defibrillator.dll`; no runtime Internet access or loose audio installation is required.
- The host chooses one of the four uniformly on every animal interval (30 seconds by default); repetitions are allowed.
- The animal timer remains at 30 seconds independently of the electrical shocks, which draw fresh 45–60 second delays by default as of 0.1.6. It starts after revival and ends on death or level exit; it continues after the 180-second comic-pitch effect expires.
- Animal playback uses a separate AudioSource at pitch 1 with Doppler disabled and the neutral native voice mixer. The assigned high/low player pitch and distorted-voice mixer are not copied.
- Real microphone/TTS playback is briefly suppressed through native volume override, without changing microphone capture, transmission settings or the microphone device.
- Speech animation/indicator fields are temporarily overlaid from the animal clip's amplitude and restored around native updates/RPCs and on cleanup. Network speech state is not forged through manual speech RPCs.
- Positional playback follows the target; local owners can hear the animal as feedback. Remote player voice gain and the native mixer remain applicable.
- Delayed packets skip elapsed clip time, expired clips are not replayed, and duplicate events are rejected by revival token and sequence. Late-join snapshots preserve the next animal deadline without replaying past sounds.

## Runtime validation still needed

Automated validation checks source WAV format/duration, embedded-byte equality, timer boundaries and native APIs. It does not establish perceived loudness, natural timbre, animation, occlusion or microphone restoration in the running game.
Test high and low assigned player voices against the same animal clip, and test push-to-talk inactive/active, TTS, spectators, mute controls, simultaneous resurrections and host/client roles.
