# Safeguards and limitations

This is an offline game, not an account/payment service. There are no API credentials, remote commands, authentication, purchases, ad SDKs or analytics SDKs in this build candidate.

Implemented in source:

- The campaign is bundled, schema-versioned and validated before gameplay. Counts, IDs, coordinate ranges, colours, force values, route budgets, non-finite numbers and delivery dependencies are checked. No data controls filesystem paths or executable code.
- Save files live in Unity's private persistent-data directory. Reads are limited to 64 KiB. Values are normalized, temporary-file replacement is atomic, and an existing backup is used after corruption. A known future schema is read-only to avoid silently downgrading another app version's save.
- Reset progress is explicitly confirmed. This does not erase a future-version save.
- Android's generated manifest is hardened by `AndroidSecurity`: application backup is disabled and cleartext traffic is prohibited. No Internet permission is forced by setup; the final merged APK manifest must still be inspected during device/build validation. Haptics may require Android's vibrate permission.
- The phone-test build does not enable Unity Development Build features. It uses the SDK's debug signing identity and is not suitable as a Play Store production signing setup. Never commit private signing keys, passwords or production credentials.
- Original mesh assets and OFL font licences are supplied. No remote or paid asset downloads occur at runtime.

A user with a rooted device, a modified APK or access to their own application files can alter offline progress. Encryption or a hard-coded HMAC key would not make that tamper-proof; no such promise is made. SHA-256 printed by the build script identifies the produced file, not an independent security audit. Dependency vulnerability review, signed-release provenance, final manifest inspection and device penetration testing have not been performed. Maintain Unity/SDK security updates before release.
