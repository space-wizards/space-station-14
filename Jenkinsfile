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
                sh 'Tools/package_server_build.py -p windows mac linux linux-arm64'
                sh 'Tools/package_client_build.py'
            }
        }
        stage('Update build info') {
            steps {
                sh 'Tools/gen_build_info.py'
                archiveArtifacts artifacts: 'release/*.zip'
            }
        }
        stage('Generate checksums') {
            steps {
                sh 'Tools/generate_hashes.ps1'
                archiveArtifacts artifacts: 'release/*.zip.sha256'
            }
        }
    }
}
