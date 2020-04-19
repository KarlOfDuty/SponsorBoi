pipeline { 
  agent any
  stages {
    stage('Build') {
      parallel {
        stage('Linux') {
          steps {
            sh 'msbuild DiscordBot.csproj -restore -t:Publish -p:OutputPath=bin/linux/ -p:BaseIntermediateOutputPath=obj/linux/ -p:TargetFramework=netcoreapp2.2 -p:SelfContained=true -p:RuntimeIdentifier=linux-x64 -p:Configuration=Release -p:DebugType=None'
          }
        }

        stage('Windows') {
          steps {
            sh 'msbuild DiscordBot.csproj -restore -t:Publish -p:OutputPath=bin/win/ -p:BaseIntermediateOutputPath=obj/win/ -p:TargetFramework=netcoreapp2.2 -p:SelfContained=true -p:RuntimeIdentifier=win-x64 -p:Configuration=Release -p:DebugType=None'
          }
        }

      }
    }

    stage('Package') {
      parallel {
        stage('Linux') {
          steps {
            sh 'mkdir Linux-x64'
            dir(path: './Linux-x64') {
              sh 'warp-packer --arch linux-x64 --input_dir ../bin/linux/publish --exec DiscordBot --output DiscordBot'
            }

          }
        }

        stage('Windows') {
          steps {
            sh 'mkdir Windows-x64'
            dir(path: './Windows-x64') {
              sh 'warp-packer --arch windows-x64 --input_dir ../bin/win/publish --exec DiscordBot.exe --output DiscordBot.exe'
            }

          }
        }

      }
    }

    stage('Archive') {
      parallel {
        stage('Linux') {
          steps {
            archiveArtifacts(artifacts: 'Linux-x64/DiscordBot', caseSensitive: true)
          }
        }

        stage('Windows') {
          steps {
            archiveArtifacts(artifacts: 'Windows-x64/DiscordBot.exe', caseSensitive: true)
          }
        }

      }
    }

  }
}
