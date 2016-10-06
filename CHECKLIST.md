### Pre-Build
* Update AssemblyInfo for ColliderHelper project
* Update `Resources\GameData\Collider-o-Scope\Collider-o-Scope.version`
* Update `Resources\GameData\Collider-o-Scope\Changelog.txt`
* Update if required `Resources\GameData\Collider-o-Scope\Readme.txt`

### Build
* Build solution in release mode
* Create zip file from `Resources\GameData\` (Collider-o-Scope folder)
* Name the zip file `Collider-o-Scope-v<major>.<minor>.<patch>.zip` (eg Collider-o-Scope-v1.0.0.zip)

### Post-Build
* Verify Github master matches local master
* Create release tag and push to Github
* Update Github release info and add binary
* Update AVC information on [KSP AVC Online](https://ksp-avc.cybutek.net/?page=My_Versions)
* Copy Github release to [Spacedock](http://spacedock.info/)
* Post update in the forum thread [KSP Forums](https://forum.kerbalspaceprogram.com/)
