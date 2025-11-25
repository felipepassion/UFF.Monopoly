pipeline {
  agent any

  parameters {
    string(name: 'IMAGE_NAME', defaultValue: 'uffmonopoly', description: 'Nome da imagem Docker')
    string(name: 'IMAGE_TAG', defaultValue: 'latest', description: 'Tag da imagem Docker')
    string(name: 'SERVER_HOST', defaultValue: '', description: 'Host do servidor destino (ex: 1.2.3.4)')
    string(name: 'SSH_CREDENTIALS', defaultValue: 'ssh-key-id', description: 'ID da credencial SSH no Jenkins (tipo SSH Username with private key)')
  }

  environment {
    PROJECT_DIR = 'UFF.Monopoly'
  }

  stages {
    stage('Checkout') {
      steps {
        checkout scm
      }
    }

    stage('Build .NET') {
      steps {
        dir("${env.PROJECT_DIR}") {
          sh 'dotnet restore'
          sh 'dotnet build --configuration Release --no-restore'
          sh 'dotnet publish -c Release -o publish --no-build'
        }
      }
    }

    stage('Docker Build') {
      steps {
        dir("${env.PROJECT_DIR}") {
          sh "docker build -t ${params.IMAGE_NAME}:${params.IMAGE_TAG} ."
          sh "docker save ${params.IMAGE_NAME}:${params.IMAGE_TAG} -o ${params.IMAGE_NAME}_${params.IMAGE_TAG}.tar"
        }
      }
    }

    stage('Transfer and load on server') {
      steps {
        withCredentials([sshUserPrivateKey(credentialsId: "${params.SSH_CREDENTIALS}", keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER')]) {
          sh """
            scp -o StrictHostKeyChecking=no -i \"$SSH_KEY\" ${env.PROJECT_DIR}/${params.IMAGE_NAME}_${params.IMAGE_TAG}.tar ${SSH_USER}@${params.SERVER_HOST}:~/UFF.Monopoly/
            ssh -o StrictHostKeyChecking=no -i \"$SSH_KEY\" ${SSH_USER}@${params.SERVER_HOST} \"mkdir -p ~/UFF.Monopoly && docker load -i ~/UFF.Monopoly/${params.IMAGE_NAME}_${params.IMAGE_TAG}.tar && rm ~/UFF.Monopoly/${params.IMAGE_NAME}_${params.IMAGE_TAG}.tar\"
          """
        }
      }
    }
  }

  post {
    always {
      cleanWs()
    }
  }
}
