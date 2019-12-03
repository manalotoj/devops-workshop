## Deploy 3-Tier app on IaaS with Azure DevOps

### Task 0: Some manual intervention
Certain things actions to be quite difficult to achieve using Terraform. One of these actions is installing the SqlIaaSAgent virtual machine extension. During testing, all works with the exception of enabling SQL server authentication. For this workshop, we will apply a manual work-around. An alternative work-around is to deploy the extension using an ARM template.

The other item is automated configuration of IIS and ASP.NET Core on Windows Server. This requires further troubleshooting. 

Nonetheless, the build and deployment process adequately deploys the 3-tier application. However, it will take some troubleshooting to address IIS. Upon successful build and deployment, verify that the SQL Server database is deployed and that an IIS application in each of the servers is deployed. Then, proceed to the joy of PaaS.

1. Enable SQL Server Authentication
- Connect to the SQL Server VM using remote desktop.
- Open SQL Server Management Studio
  - Connect to the "local" SQL server
  - Enable SQL server authentication
    - Right-click the SQL server, select "Properties", select "Security".
    - Under "Server authentication", click "SQL Server and Windows Authentication mode", click "OK".
    - Right-click the SQL server and select "Restart", accept all prompts.
2. Create a username and password
- under the local SQL server, expand Security>Logins
 - Right-click "Logins" and select "New Login". Set the following properties:
   - Login name
   - Select SQL Server authentication
   - Password
   - Uncheck "Enforce password policy".
   - In the "Server Roles" pane, select "sysadmin".
  - Note: For simplicity, we will use this same login to manage the database and to connect from an application. In practice, you would use separate credentials and apply least privilege.

### Task 1: Modify your build pipeline
1. Add the **source** and the **arm-templates" folders from the devops-workshop repo to the root of your repo. Commit and push changes.
2. Add the following pipeline variables.

    | Variable Name | Value | Scope       |
    | ------------- | ----- | ----------- |
    | environment   | dev   | Development |
    | environment   | stg   | Staging     |

**Add the following new tasks at the very beginning of your job agent.**

2. Add a "NuGet tool installer" task. Use all defaults.
3. Add a task and enter "Nuget restore"' in the search textbox. Use all defaults.
4. Add a "Visual Studio build" task to build the TodoApp project. Set the following properties:
   - Display name: Build Web App Project
   - Solution: source/TodoListApp/TodoApp/TodoApp.csproj
   - MSBuild Arguments: /p:DeployOnBuild=true /p:WebPublishMethod=Package /p:PackageAsSingleFile=true /p:SkipInvalidConfigurations=true /p:DesktopBuildPackageLocation="$(build.artifactstagingdirectory)\source\TodoApp\TodoApp.zip" /p:DeployIisAppPath="Default Web Site"
   - Platform: $(BuildPlatform)
   - Configuration: $(BuildConfiguration)
5. Clone the "Visual Studio build" task from step 4. Set the following properties:
   - Display name: Build Web API Project
   - Solution: source/TodoListApp/TodoListApi/TodoListApi.csproj
   - MSBuild Arguments: /p:DeployOnBuild=true /p:WebPublishMethod=Package /p:PackageAsSingleFile=true /p:SkipInvalidConfigurations=true /p:DesktopBuildPackageLocation="$(build.artifactstagingdirectory)\source\TodoApi\TodoApi.zip" /p:DeployIisAppPath="Default Web Site"
   - Platform: $(BuildPlatform)
   - Configuration: $(BuildConfiguration)
6. Add a "Visual Studio build" task to build the TodoDb project. Set the following properties:
   - Display name: Build TodoDb Database Project
   - Solution: source/TodoListApp/TodoDb/TodoDb.sqlproj
   - Platform: $(BuildPlatform)
   - Configuration: $(BuildConfiguration)
7. Add a "Copy files" task. Set the following properties:
   - Display name: Copy SQL DACPAC
   - Source folder: $(system.defaultworkingdirectory)
   - Contents: **\bin\Output\**
   - Target folder: $(build.artifactstagingdirectory)/source/TodoDb
8. Add a "Copy files" task to copy the arm-templates folder to the build artifacts.
   - Display name: Copy ARM templates
   - Source folder: $(system.defaultworkingdirectory)
   - Contents: **\deployment-groups\**
   - Taret folder: $(build.artifactstagingdirectory/source/arm-templates).
9.  Queue a build and ensure the following upon successful completion that the build artifacts contain: 
   - /source/TodoApi/TodoApi.zip
   - /source/TodoApp/TodoApp.zip
   - /source/TodoDb/TodoDb.dacpac
   - /source/arm-templates

### Task 2: Create a deployment group
Deploying IaaS to Azure VMs requires the creation of a [deployment group](https://docs.microsoft.com/en-us/azure/devops/pipelines/release/deployment-groups/?view=azure-devops). An alternative is to use a WinRM deployment.
- Hover over pipelines and select "Deployment groups".
- Click new, provide a "Deployment group name", and click "Create".

### Task 3: Use Azure resource group deployment task to provision agents for the deployment group.
1. Within Azure DevOps, generate a personal access token (PAT). This is required in order to provision agents for the deployment group.
2. Modify your release pipeline to provision a VM agent for the backend VM.
   - within the Development stage, add an "Azure Resource Group deployment" task to the very end. Set the following properties:
     - Display name: add backend VM to deployment group
     - Azure subscription: specify your subscription
     - Action: create or update resource group
     - Resource group: your dev resource group
     - Lication: your dev resource group's location
     - Template location: click on ellipsis and navigate to ../template.json
     - Templatel parameters: click on ellipsis and navigate to ../parametere.json
     - Override template parameters: click on the ellipsis and provide a value for each template parameter (if the parameters are not rendered, enter the names manually - note that they are case-sensitive).
       - For "Tags", specify "backend".
3. Repeat step 2 for the frontend VM (set "Tags" to "frontend") and the middle tier VM (set "Tags" to "middletier"). Tag values are arbitrary but they must be unique.

### task 4: Modify your release pipeline to deploy the 3-tier application
1. Add a stage after the Development stage. Rename it "Dev-App".
2. Delete the existing "Agent job".
3. Add a "Deployment group job" by clicking on the stage's ellipsis. 
   - Specify the deployment group created earlier in the "Deployment group" dropdown. 
   - Name it "IIS Deployment".
   - Specify "frontend" for "Required tags"
   - Add the following tasks:
      - "IIS web app manage" task. Set the following properties:
        - Display name: IIS Web App Manage: Todo API
        - Check "Enable IIS"
        - Under "IIS Web Application":
          - Parent website name: Default Web Site
          - Virtual path: /todoapi
          - check "Create or update app pool"
        - Under IIS Application pool:
          - Name: todoApiAppPool
          - .NET version: v4.0
    - "IIS web app deploy" task. Set the following properties:
      - Dsiplay name: IIS Web App Deploy: Todo API
      - Virtual application: /todoapi
      - Package or folder: $(System.DefaultWorkingDirectory)\**\TodoApi.zip
    - Use a "Replace Tokens" task to set the connection string.
4. Repeat step step 3 to add a corresponding deployment for the Todo Web App.
    - Specify "middletier" for "Required tags"
    - Use a "Replace Tokens" task to set the connection API endpoint app setting.
5. Add another "Deployment group job" as in step 3. 
    - Name it "SQL Deployment".
    - pecify "backend" for "Required tags".
    - Add a "SQL Server database deploy" task. Name it "SQL DB Deploy".
    - Set the following properties:
      - DACPAC file: **\*.dacpac
      - Authentication: SQL Server Authentication
      - SQL Username: $(sqlUsername)
      - SQL Password: $(sqlPassword)
      - Note: a corresponding pipeline variables and values must be defined for this stage.
6. Adjust the deployment such that the "Staging" stage is triggered by the "Dev-App" stage.
7. Create a release and verify that your pipeline's development stages completes successfully.
   - Verify that a database containing a TodoItems table is created in the backend VM.
   - Verify that the correct web app/api is created in the frontend VM and the middletier VM.