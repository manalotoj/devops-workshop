## Deploy 3-Tier app on PaaS with Azure DevOps

In this section, you will deploy the TodoList App to Azure PaaS. You will take the following steps to achieve this:
- Create the following Azure services:
  - App service plan
  - App service for the frontend
  - App service for the api
  - SQL Database 
  
In practice, you would achieve this task using either ARM templates or Terraform scripts and incorporate it as part of your deployment. For this section, the purpose is not IaC but to provide you with the experience of deploying an n-tier application to Azure. As such, you are free to establish your Azure environment in any way you wish (ex. via Azure portal, Azure CLI, PowerShell, ARM templates, or Terraform).

### Task 1: Establish Azure environment

- In your subscrition, create the following:
  - Create a resource group; make note of the name. Create all services within this resource group.
  - App service for the frontend web app; make note of the name
  - App service for the web API; make note of the name. Enable CORS support.
  - Create a SQL Database instance.

### Task 2: Update the TdoDb SQL Project's target database engine.
- Navigate to /source/TodoDB and edit the TodoDB.sqjproj file.
- Replace Sql130DatabaseSchemaProvider with SqlAzureV12DatabaseSchemaProvider.
- Save, commit, and push your changes.
- Trigger a build.

### Task 3: Create a release pipeline
1.  Within your Azure DevOps project, hover over the Pipelines icon and select release.
2. Click link to start with an "Empty job"
3. In the Artifacts pane
    - Change the name to workshop-CD
		- Add an artifact and select the previously created build; accept all defaults and click Add.
		- Rename "Stage 1" to "Development".
4. Add the following pipeline variables:
    | Variable Name | Value                 | Scope       |
    | ------------- | --------------------- | ----------- |
    | sqlPassword   | \<your-sql-password\> | Development |
    | sqlUsername   | \<your-sql-username>  | Developemnt |
- Click the lock icon on the sqlPassword to secure its value. In practice, this can be secured using [variable groups](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/variable-groups?view=azure-devops&tabs=yaml) Azure KeyVault.

5. Add an "Azure App Service deploy" task for the web API. Set the following properties:
   - Display name: Azure App Service Deploy: TodoApi
   - Connection type: Azure Resource Manager
   - Azure subscription: <your-subscripton>
   - App service type: Web App on Windows
   - App Service name: <your-api-app-service-name>
   - Package or folder: $(System.DefaultWorkingDirectory)/**/TodoApi.zip
5. Add an "Azure App Service deploy" task for the frontend web app. Set the following properties:
   - Display name: Azure App Service Deploy: TodoApp
   - Connection type: Azure Resource Manager
   - Azure subscription: <your-subscripton>
   - App service type: Web App on Windows
   - App Service name: <your-api-app-service-name>
   - Package or folder: $(System.DefaultWorkingDirectory)/**/TodoApp.zip
   - Application and Configuration Settings>App settings:
  ```
  -TodoApiEndpoint "https://<your-api-app-service-name>.azurewebsites.net/api/"
  ```
6. Add an Azure CLI task to set the connection string. Set the following properties:
   - Display name: Azure CLI
   - Azure subscription: <your-subscription>
   - Script Location: Inline script
   - Inline Script:
  ```
az webapp config connection-string set -g workshop-rg -n workshop-todo-api -t SQLAzure --settings TodoDb="Server=tcp:<your-sql-database-server>,1433;Initial Catalog=todo;Persist Security Info=False;User ID=$(sqlUsername);Password=$(sqlPassword);MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  ```
7. Create a new release and verify that the app is in working order. Troubleshooting steps:
   - Did your database table get created? Look for a TodoItems table.
   - Test the api by navigating to https://<your-api-app-service-name>.azurewebsites.net/api/todoapis. This should display json data (it will return [] if empty).
     - If it's not working, check your api app service and verify that the connection string has been configured.
   - If the web app is not working, check ythe app service and verfiy that an app setting named "TodoApiEndpoint" has been configured.