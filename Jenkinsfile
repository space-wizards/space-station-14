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
                sh './package_release_build.py'
                archiveArtifacts artifacts: 'release/*.zip'
                archiveArtifacts artifacts: 'engine/Resources/ResourcePack.zip'
            }
        }
    }
}
