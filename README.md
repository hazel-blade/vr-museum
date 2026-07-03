# VR Museum

A Unity project built with **Unity 2022.3.62f1 (LTS)**. The repository uses
[Git Large File Storage (Git LFS)](https://git-lfs.com/) for large assets such
as models, textures, audio, and Unity asset files.

## Prerequisites

- Git
- Git LFS
- [Unity Hub](https://unity.com/download)
- Unity Editor **2022.3.62f1**

## Install Git LFS

Git LFS must be installed before cloning so that Unity assets are downloaded
instead of LFS pointer files.

### Windows

Install Git LFS using one of these methods:

```powershell
# Windows Package Manager
winget install GitHub.GitLFS
```

Alternatively, download and run the Windows installer from
[git-lfs.com](https://git-lfs.com/). Git LFS is also included with recent
versions of [Git for Windows](https://gitforwindows.org/).

Then open a new PowerShell or Git Bash window and initialize Git LFS:

```powershell
git lfs install
git lfs version
```

### macOS

Install Git LFS with [Homebrew](https://brew.sh/):

```bash
brew install git-lfs
git lfs install
git lfs version
```

If Homebrew is not installed, download the macOS package from
[git-lfs.com](https://git-lfs.com/) and run `git lfs install` afterward.

## Clone the Project

```bash
git clone https://github.com/hazel-blade/vr-museum.git
cd <project-name>
git lfs pull
```

The final `git lfs pull` ensures all large assets are available locally. You
can verify them with:

```bash
git lfs ls-files
```

## Open and Run

1. Open **Unity Hub**.
2. Install Unity Editor **2022.3.62f1** if it is not already installed. In
   Unity Hub, choose **Installs > Install Editor > Archive** to locate this
   exact version.
3. Select **Projects > Add > Add project from disk** and choose the cloned
   `<project-name>` folder.
4. Open the project and wait for Unity to import assets and restore packages.
5. In the Project window, open `Assets/Scenes/Sample Scene.unity`.
6. Press the **Play** button at the top of the Unity Editor.

If Unity shows missing or invalid assets, close Unity, run `git lfs pull` in
the project directory, and reopen the project.
