## Terraform and Azure DevOps Hands-On
In this section, you will use Azure DevOps to create CICD pipelines to build and deploy the Terraform configurations you created in the previous section. 

### Task1: Create a build pipeline
1. Within your Azure DevOps project, hover over the Pipelines icon and select Builds.
2. When prompted with "Where is your code?", you have an option to use a YAML build, or, at the very bottom, "Use the classic editor…" to create a pipeline without YAML. Unless you are familiar with pipelines, choose the latter option.
3. In "Select a source", accept the defaults which will be:
- Azure Repos Git
- Team project: the project you created
- Repository: the default repository within the project
- Default branch for manual and scheduled builds: master
- Click Continue

4. Click link to start with an "Empty job"
5. Change the name to workshop-CI
6. In the Triggers tab, check "Enable Continuous Integration"; review other options.
7. Under "Agent job 1"
  - Agent pool: use Azure Pipelines
	- Agent Specification: vs2017-win2016]

**Add tasks to the agent**

8. Add a Copy Files task
  - In the add tasks search box, enter "copy files". Select it in the search results and click the add button.
  - Configure the following properties:
    - Source folder: use the ellipsis to select the "main" folder of your tf scripts.
    - Target folder: $(build.artifactstagingdirectory)/terraform
9.  Add a Publish Pipeline Artifacts task
  - In the Add tasks search box, enter "publish pipeline artifacts" files. Select it in the search results and click the Add button.
	- Configure the following settings:
      - Source folder: $(build.artifactstagingdirectory)/terraform
      - Artifact name: drop
      - Artifact publish location: Azure Pipelines
10. Save the pipeline and queue a new build. Verify that the build succeeds and contains artifacts (tf script files)

### Task 2: Create a release pipeline
1.  Within your Azure DevOps project, hover over the Pipelines icon and select release.
2. Click link to start with an "Empty job"
- In the Artifacts pane
    - Change the name to workshop-CD
		- Add an artifact and select the previously created build; accept all defaults and click Add.
		- Click the lightning bolt icon and enable the "Continuous deployment trigger".
		- Rename "Stage 1" to "Development".

**Add tasks to the Development stage**

3. Add a task and search for "Terraform Build & Release Tasks". This is a marketplace task and must be added to your Azure DevOps account.
   
- Save your pipeline as you may have to refresh the page for the terraform tasks to become available.
4. Add a task and search for "Terraform installer". Specify version 0.12.16.
5. Add a Terraform CLI task to initialize state
- Command: select **init** from the dropdown.
- Configuration directory: use the ellipsis to specify the root folder of the tf scripts.
6. Add a Terraform CLI task to apply
- Command: select **apply** from the dropdown.
- Configuration directory: use the ellipsis to specify the root folder of the tf scripts.
- Provide an Azure Environment Subscription. Select from dropdown list to use an existing connection. Click on "Manage" to create a new connection.
7. Create and run a release.
- Ensure that the pipeline completes successfully.
- View execution logs. Notice that no changes were made to existing resources.
	
8. Trigger your build pipeline
- Trigger your build pipeline by making a change to one of your terraform templates. Ex. add an environment tag to the vnet in vnet.tf.
	  tags = {
	    environment = "Development"
	  }
		a. Commit your change and verify that a new build is now in progress.
- Verify that the completion of the build triggers a new release.
- Upon completion of the release, verify that your tag has been applied by viewing the VNET's tags within the Azure portal.

### Task 3: Add a Staging environment to the release pipeline
In this task, you will add a staging environment to your release pipeline. You will also introduce a manual approval step prior to execution. In practice, automated integration tests can take the place of this manual approval.

1. disable continuous integration trigger for your build pipeline (or one will be triggered each time you push changes)

2. create file named variables.tfvars in the root folder of your terraform scripts. Add the below contents, then tsave, commit, and push to repo.
``` terraform
	environment = "__environment__"
```
3. make the following changes in main.tf (this will tokenize the environment for the state store):

```terraform
terraform {
  required_version = ">=0.12"
  backend "azurerm" {
    storage_account_name  = "<your-storage-account-name>"
    container_name        = "state-__environment__"
    key                   = "key-__environment__"
    access_key            = "<storage-account-key>"
  }
}
```
4. Add a stage to your release pipeline
- Add a stage after Development by doing the following:
  - Clone the existing stage and change the name to "Staging".
			§ In the "pre-deployment conditions", enable "Pre-deployment approvals" and add your credentials to the "Approvers".
  - In the Variables pane, add the following variables:

    | Variable Name | Value | Scope       |
    | ------------- | ----- | ----------- |
    | environment   | dev   | Development |
    | environment   | stg   | Staging     |

- Add a "Replace Token" task to each stage
  - In the Tasks pane for the Development stage
    - Add a new "Replace Tokens" task (install from marketplace, select Replace Tokens authored by Guillaume Rouchon)
		- Ensure that this is the first task in the job (drag and drop)
		- apply the following:
    		- Root directory: leave blank to use the working directory.
  			- Target files: **/*.tfvars, **/*.tf
    		- Advanced settings: set Token prefix and Token suffix to **__** (two underscore characters)
  - Repeat for the Staging stage
- For each stage, modify Terraform tasks and provide the following additional command options
  - var-file="variables.tfvars"
	- Note: if you run into unexpected errors in the Staging stage, your state may be corrupt. Verify that you are using a different container to store the state. Consider deleting any files within the container for staging state.
5. Trigger a build manually
- Verify that the build pipeline completes successfully
- Verify that the release pipeline completes successfully
- verify that a new environment is created in the Azure portal

### Task 3 (Optional): Create a pipeline to teardown resources
This is convenient to have for testing. Use with care.
1. Clone your release pipeline, disable any/all gates.
2. Replace the tf **apply** tasks with **destroy**.
3. Test your pipeline.

