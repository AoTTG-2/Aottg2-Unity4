# Aottg2-Unity4

### About
Aottg2 is the sequel to the original Attack on Titan Tribute Game created by FengLee. This project is currently built in Unity 4, with plans to migrate to modern versions of Unity. For more details, join our discord server: https://discord.gg/GhbNbvU.

Running the game: the current build can be found in the Aottg2 folder. Open the Aottg2.exe file to start the game. Note that this is a
development build and not a release build.

Contributing: join our discord server for more details on how to contribute. We accept applicants for a variety of work including programming, 3D modeling, texture and 2D art, sound effects, music, translation, and more.

### Build process
The project is not yet a complete Unity project, with the code and unity assets being compiled separately. The Assembly folder contains all scripts, AssetBundle contains all unity assets, and Aottg2 contains the current build. In order to create a build, we first compile the scripts from the Assembly folder into a dll file (Aottg2/Aottg2_Data/Managed/Assembly-CSharp.dll). Next, we compile the unity assets into an assetbundle (Aottg2/Aottg2_Data/MainAssets.Unity3d). Once these two files are replaced, the build is complete.

### Required installs
1. Install git: https://git-scm.com/book/en/v2/Getting-Started-Installing-Git
2. Install git lfs: https://git-lfs.github.com/
3. If modifying code, install visual studio: https://visualstudio.microsoft.com/vs/
4. If modifying unity assets, install Unity 4.7.2: https://unity3d.com/get-unity/download/archive (you will need pro version to build and test asset bundles on your local machine, but this is not necessary to contribute to the project)

### Downloading the project
1. Open command prompt and [navigate](https://www.howtogeek.com/659411/how-to-change-directories-in-command-prompt-on-windows-10/) to your preferred installation folder
2. Enter `git clone https://github.com/AoTTG-2/Aottg2-Unity4.git`
3. To open the assembly scripts, navigate to Aottg2-Unity4/Assembly and open the Assembly-CSharp.sln using Visual Studio
4. To open the asset bundle, open up Unity 4.7.2 and select the Aottg2-Unity4/AssetBundle folder

### Keeping project updated
1. Navigate your command prompt to the project folder (Aottg2-Unity4 folder)
2. Switch to your assigned branch by using `git checkout branch_name`
3. Enter `git pull` to update the project to the latest version

### Making and uploading changes
1. Navigate your command prompt to the project folder (Aottg2-Unity4 folder)
2. Switch to your assigned branch by using `git checkout branch_name`
3. Modify or add files to either Assembly or AssetBundle folders
4. Make sure your project is updated to the latest version by using `git pull`
5. Enter `git status` to see which files have been changed, added, or removed by you
6. Add the files changes you wish to upload by entering `git add FILE`, or enter `git add .` to add all changes
7. Enter `git commit -m "Message"` to commit the changes, replace Message with your change description but include the quotation marks
8. Enter `git push` to finally upload the changes

### Building and testing assembly
1. Open the assembly scripts by navigating to Aottg2-Unity4/Assembly and opening Assembly-CSharp.sln
2. Make your desired changes to the scripts
3. Build the assembly by clicking Build -> Build Assembly-CSharp or by pressing Ctrl+B
4. The Aottg2/Aottg2_Data/Managed/Assembly-CSharp.dll file should now be updated and ready to play

### Building and testing asset bundle
1. Open the AssetBundle by opening Unity 4.7.2 and selecting the Aottg2-Unity4/AssetBundle folder
2. Make your desired changes to the unity assets
3. In Unity, right click on the "MainAssets" folder and click "Build asset bundle from selection - Track dependencies"
4. Save as MainAssets.unity3d to the Aottg2/Aottg2_Data folder, and overwrite the file when prompted.
5. The Aottg2/Aottg2_Data/MainAssets.unity3d file should now be updated and ready to play
  
### Privacy
1. By default, github exposes your username and github email address when you make commits and push changes.
2. To block any commits that exposes public email, follow this guide: https://docs.github.com/en/github/setting-up-and-managing-your-github-user-account/managing-email-preferences/blocking-command-line-pushes-that-expose-your-personal-email-address
3. After this, you need to update your git config email to use a private no-reply one. Follow this guide to do this: https://docs.github.com/en/github/setting-up-and-managing-your-github-user-account/managing-email-preferences/setting-your-commit-email-address
