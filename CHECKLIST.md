### Pre-Build
* Update AssemblyInfo for ColliderHelper project
* Update `Resources\GameData\Collide-o-Scope\Collide-o-Scope.version`
* Update `Resources\GameData\Collide-o-Scope\Changelog.txt`
* Update if required `Resources\GameData\Collide-o-Scope\Readme.txt`

### Build
* Build solution in release mode
* Create zip file from `Resources\GameData\` (Collide-o-Scope folder)
* Name the zip file `Collide-o-Scope-v<major>.<minor>.<patch>.<build>.zip` (eg Collide-o-Scope-v1.0.0.0.zip)

### Post-Build
* Verify Github master matches local master
* Create release tag and push to Github
* Update Github release info and add binary
* Update AVC information on [KSP AVC Online](http://ksp-avc.cybutek.net/?page=My_Versions)
* Post update in the forum thread [KSP Forums](http://forum.kerbalspaceprogram.com/index.php?/topic/149706-12-collide-o-scope-v100/)
