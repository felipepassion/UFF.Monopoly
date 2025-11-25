pipeline {
    agent any

    environment {
        SSH_HOST = '72.60.5.211'
        SSH_USER = 'devinfomarca'
        REMOTE_REPO_PATH = '~/UFF.Monopoly'
        DEPLOY_PATH = '/UFFMonopoly/volumes/WebApp'
    }

    stages {
        stage('Prepare Environment') {
            steps {
                script {
                    echo "Attempting to connect to ${SSH_USER}@${SSH_HOST}..."
                    sshagent(['a43db6b7-3ece-498c-b200-8a0c8be24bcf']) {
                        sh '''
                        # Test SSH connection first
                        ssh -o BatchMode=yes -o ConnectTimeout=10 -o StrictHostKeyChecking=no ${SSH_USER}@${SSH_HOST} 'echo "SSH connection successful"' || {
                            echo "ERROR: Cannot establish SSH connection to ${SSH_USER}@${SSH_HOST}"
                            echo "Please verify:"
                            echo "1. The SSH key is added to ~/.ssh/authorized_keys on the remote server"
                            echo "2. The Jenkins credential 'a43db6b7-3ece-498c-b200-8a0c8be24bcf' contains the correct private key"
                            echo "3. The remote server is accessible from Jenkins"
                            exit 1
                        }
                        
                        ssh -o StrictHostKeyChecking=no ${SSH_USER}@${SSH_HOST} bash -s <<'REMOTE_EOF'
set -e  # Exit on any error
echo "Cleaning up old repository..."
sudo rm -rf ~/UFF.Monopoly || true

echo "Cloning repository..."
git clone git@github.com:felipepassion/UFF.Monopoly.git ~/UFF.Monopoly

echo "Setting permissions..."
sudo chown -R ${SSH_USER}:${SSH_USER} ~/UFF.Monopoly
sudo chmod -R 755 ~/UFF.Monopoly

echo "Updating repository..."
cd ~/UFF.Monopoly
git pull origin master --force

echo "Repository prepared successfully"
REMOTE_EOF'''
                    }
                }
            }
        }

        stage('Restore Dependencies') {
            steps {
                script {
                    sshagent(['a43db6b7-3ece-498c-b200-8a0c8be24bcf']) {
                        sh '''ssh -o StrictHostKeyChecking=no ${SSH_USER}@${SSH_HOST} bash -s <<'REMOTE_EOF'
set -e
cd ~/UFF.Monopoly
echo "Restoring .NET dependencies..."
sudo ~/dotnet/dotnet restore
echo "Dependencies restored successfully"
REMOTE_EOF'''
                    }
                }
            }
        }

        stage('Build Environment') {
            steps {
                script {
                    sshagent(['a43db6b7-3ece-498c-b200-8a0c8be24bcf']) {
                        sh '''ssh -o StrictHostKeyChecking=no ${SSH_USER}@${SSH_HOST} bash -s <<'REMOTE_EOF'
set -e
echo "Cleaning deployment directory..."
sudo rm -rf /UFFMonopoly/volumes/WebApp/* || true

echo "Building and publishing application..."
cd ~/UFF.Monopoly
sudo ~/dotnet/dotnet publish --os linux --arch x64 -p:PublishTrimmed=false -o /UFFMonopoly/volumes/WebApp
echo "Build completed successfully"
REMOTE_EOF'''
                    }
                }
            }
        }

        stage('Deploy via Portainer') {
            steps {
                script {
                    sshagent(['a43db6b7-3ece-498c-b200-8a0c8be24bcf']) {
                        sh '''ssh -o StrictHostKeyChecking=no ${SSH_USER}@${SSH_HOST} bash -s <<'REMOTE_EOF'
set -e
echo "Stopping and removing existing container..."
sudo docker stop UFFMonopoly-webapp || true
sudo docker rm UFFMonopoly-webapp || true

echo "Deploying with docker-compose..."
cd ~/docker-uff/docker-uff
sudo docker-compose up -d --build

echo "Verifying deployment..."
sudo docker ps -a | grep UFFMonopoly-webapp
echo "Deployment completed successfully"
REMOTE_EOF'''
                    }
                }
            }
        }
    }
}