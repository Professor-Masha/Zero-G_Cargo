# Windows build: Out of memory

The reported laptop errors include BuildProgram terminating with Out of memory and shader compilation returning 0x8007000e. These establish memory exhaustion during build/import work; they do not establish a gameplay shader defect. Task Manager also showed high RAM use and 100% disk activity, which can accompany heavy paging.

1. Do not start another APK build while Background Tasks is compiling shaders or importing assets. The build menus now reject busy editors, the wrong target, Play mode, an existing player build and known script compilation failures.
2. Save any wanted scene/project changes, close Unity, Android Studio, unused browsers and other memory-heavy applications. Restart Windows if memory is not released. Reopen only Unity and the project; let imports and background compilation finish before another build attempt.
3. Check Windows virtual memory: press Win+R, run `sysdm.cpl`, open Advanced → Performance Settings → Advanced → Virtual memory Change. Prefer **Automatically manage paging file size for all drives**, or a system-managed page file on a drive with adequate free space. Apply and restart Windows if changed. A page file can help commit limits but is slower than RAM and does not guarantee a successful build.
4. Do not delete Library as the first response; doing so forces a full reimport and more compilation. Do not disable antivirus or shader verification to mask the failure.
5. Once the editor is idle and red compiler errors have been investigated, close the interactive editor and try the provided headless script. For the externally installed editor path shown on the laptop:

```powershell
.\scripts\build-phone-apk.ps1 -UnityEditor "C:\Program Files\Unity 6000.6.7f1\Editor\Unity.exe"
```

The script uses `-batchmode -nographics -job-worker-count 2`. This removes the interactive graphics window and reduces Unity job-worker concurrency; it is not a universal shader compiler/Gradle memory cap. Custom installed SDK/JDK/NDK paths must already be configured in that user's Unity preferences.

6. If failure persists, collect `Builds/phone-build.log` and the first failing block from `%LOCALAPPDATA%\Unity\Editor\Editor.log`. Report total RAM and Task Manager's Memory → Committed value as well. Separate C# compiler errors from the existing shader/build OOM errors. Sustained failures may require more RAM or another build machine.

The screenshot's Gradle/JVM heap setting is not evidence that Gradle caused the failure; the observed errors are from Unity's build and shader processes. Do not raise heap limits on a memory-constrained machine. The cloud cannot reproduce the laptop's Unity build, so these changes are mitigations and clearer preconditions, not a verified APK build fix.
