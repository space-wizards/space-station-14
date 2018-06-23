pipeline {
    agent any

    stages {
        stage('Setup') {
            steps {
                sh 'git submodule update --init --recursive'
                sh 'TMP=~/.cache/NuGet/ nuget restore'
                sh 'engine/Tools/download_godotsharp.py'
            }
        }
        stage('Build') {
            steps {
                sh './package_release_build.py -p windows mac linux --godot /home/pjbriers/builds_shared/godot --windows-godot-build /home/pjbriers/builds_shared/win --linux-godot-build /home/pjbriers/builds_shared/linux'
                archiveArtifacts artifacts: 'release/*.zip'
            }
        }
    }
}
