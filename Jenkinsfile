pipeline {
    agent any

    stages {
        stage('Setup') {
            steps {
                sh 'git submodule update --init --recursive'
            }
        }
        stage('Build') {
            steps {
                sh 'Tools/package_release_build.py -p windows mac linux'
                archiveArtifacts artifacts: 'release/*.zip'
            }
        }
    }
}
