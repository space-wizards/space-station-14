pipeline {
    agent any

    stages {
        stage('Setup') {
            steps {
                sh './RUN_THIS.py'
            }
        }
        stage('Build') {
            steps {
                sh './package_release_build.py --platform windows linux'
                archiveArtifacts artifacts: 'release/*.zip',
            }
        }
    }
}
