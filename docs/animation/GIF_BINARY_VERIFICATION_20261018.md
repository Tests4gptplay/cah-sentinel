# Five candidate GIF binaries: remote verification

Public GitHub `main` was pushed without force and re-fetched into an independent bare Git object database.

Verified commit: `301b497b521d528307f0082364352d427f7437f1`. Verification read each remote-fetched blob, checked GIF87a/GIF89a magic bytes, byte count and SHA-256 against `manifest.v0.4.json`. These are binary Git blobs, not LFS pointers or text inventories.

Archive: 3,201,546 bytes; SHA-256 `2e50b81ddd48e03408a90ca9159b5d5f001dd60b86f13af99c33001093ba6bff`; ZIP CRC and all five content hashes passed before byte-preserving import.

| Path under assets/animation-candidates/ | Bytes | SHA-256 | Result |
| --- | ---: | --- | --- |
| `states/idle/loop.gif` | 222191 | `0cac2e27bcb88e971dcdecbd5b44521b28e6bbf41ad12ea5702935885f98bc0c` | PASS |
| `states/working/loop.gif` | 321348 | `cbebe799cc7e52dfc1e01adc651a34b0e787d4f12a26561d659514c9ba6c4687` | PASS |
| `states/wait_user/loop.gif` | 356485 | `2be3d01c92610da14595799fdd1e68f1109f9af2e8194d920bda7a3062b82f62` | PASS |
| `states/error/loop.gif` | 157591 | `0e9fe898dd2aec8084c8fafbef511baebbb89b53ff1f3e5bb7739317a5b9a5dc` | PASS |
| `states/complete/once.gif` | 1157349 | `cbcb81d3656b016e9d6e96e9adbf5512afa171f2e875899aad3e7aca57061f6c` | PASS |

Art attribution: `assets/animation-candidates/ATTRIBUTION.md`; license: `assets/animation-candidates/LICENSE-ART.md` and root `ASSET_LICENSE.md`. ZipZipPipe character/AI derivatives remain CC BY-NC-SA 4.0, separate from AGPL-3.0-only code. Status: **candidate_unreviewed**. No private CAH repository access, release API calls, tags, or existing v0.0.2 release modifications were performed. Binary archiving does not complete GF-02 or M1.
