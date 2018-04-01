pipeline {
    agent any

    stages {
        stage('Setup') {
            steps {
                sh 'git submodule update --init --recursive'
                sh 'TMP=~/.cache/NuGet/ nuget restore'
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
