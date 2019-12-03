## Policy and Compliance

In this task, you will modify your release pipeline to include policy and compliance. Prior to deploying your infrastructure, you will create Azure policies. Having these in place ensures that your deployments remain in compliance. This will require the following steps:
- defining policies (in practice, review the built-in policy definitions prior to creating custom definitions).
- updating your build to include build artifacts
- assigning policies to a particular scope.
  - In this workshop, you will scope policies to a resource group.
  - You will assign a custom policy as well as a built in policy. When assigning a built-in policy, all you need is the policy name.

Note: Bash scripts that make defining and assigning policies more convenient. As such, the instructions to follow use a Linux agent and Azure CLI (bash). You are free to choose Windows and PowerShell as an alternative; however, doing so will make the bash scripts unusable unless first converted to PowerShell.

**References:**
- [Azure Policies Overview](https://docs.microsoft.com/en-us/azure/governance/policy/overview)
- [Azure Policy Samples](https://docs.microsoft.com/en-us/azure/governance/policy/samples/)
- [Quickstart: Create a policy assignment to identify non-compliant resources using Azure PowerShell](https://docs.microsoft.com/en-us/azure/governance/policy/assign-policy-powershell)
- [Quickstart: Create a policy assignment to identify non-compliant resources with Azure CLI](https://docs.microsoft.com/en-us/azure/governance/policy/assign-policy-azurecli)


### Task 1: Modify your build pipeline

1. Add the **policies** folder from the devops-workshop repo to the root of your repo. Commit and push changes.
2. Modify your build to include the **policies** folder.
   - Add a another "Copy files" task and copy the contents of the policies folder to the staging directory (just as was done for tf scripts). Set the following property:
     - Source folder: 
     - Target folder: $(build.artifactstagingdirectory)/policies
3. Generate a new build and ensure that a policies folder is now included in your build artifacts.

### Task 3: Modify your release pipeline

1. In your release pipeline, add a Linux agent to the Development stage (note: if you do not wish to use bash scripts, this step is not required.)
   -  In the **Development** stage, add another agent and configure as follows:
      - Agent pool: Azure pipelines
      - Agent specification: Ubuntu-18.04
      - Ensure that this is the first agent, with the existing one, second in order (use drag-and-drop).
      - Repeat these steps for the **Staging** stage. 
2. Add tasks to the new agent. Note: If using PowerShell, add these taks at the very beginning.

  - **Add a task to define and assign a custom policy that specifies allowed locations.** 
    - In the add tasks search box, enter "AZ CLI". select "Azure CLI" from the results and click add.
    - Set the following properties:
      - Display name: Create and assign allowed-locations-policy
      - Azure subscription: provide a connection to your subscripton
      - Script Location: Inline script
      - Inline Script: pase the contents of [create-and-assign-custom-policy.sh](https://raw.githubusercontent.com/manalotoj/devops-workshop/master/policies/create-and-assign-custom-policy.sh).
      - Arguments:
      ```
      scope_rg=<dev-resource-group-name> policy_name='allowed-locations-policy' path=$(System.DefaultWorkingDirectory)/_<your-release-pipeline-name>/policies/allowed-locations
      ```
  - **Add a task to assign a built-in policy that specifies allowed virtual machine SKUs**
    - Add another "Azure CLI" task.
    - Set the following properties:
      - Display name: Create and assign allowed-locations-policy
      - Azure subscription: provide a connection to your subscripton
      - Script Location: Inline script
      - Inline Script: pase the contents of [assign-built-in-policy.sh](https://raw.githubusercontent.com/manalotoj/devops-workshop/master/policies/assign-built-in-policy.sh).
      - Arguments:
      ```
      scope_rg=dev-main-rg policy_name=cccc23c7-8427-4f53-ad12-b6a63eb452b3 name='allowed-vm-skus-policy-assignment' display_name='allowed VM SKUs policy assignment' path=$(System.DefaultWorkingDirectory)/_<your-release-pipeline-name>/drop/policies/allowed-vm-skus
      ``` 
    - Note: If you have completely destroyed your environment, you may run into a chicken-before-the-egg situation. This will require you to create the resource group prior to creating a release.
3. Repeat step 5 for the **Staging** stage.
4. Add a **Post deployment condition** to the Development stage
   - In the Development stage, click on the Post deployment Conditions and enable "Gates.
     - Specify 0 minutes before evaluation
   - Add a new deployment gate to **Check Policy Compliance**.
5. Repeat step 7 for the **Staging** stage.
6. Test your deployment. If it succeeds with no issue, make it fail by modifying your tf configuration file(s) to either deploy to a region that is not in the allowed regions list, or, change one of the VM SKUs to an unapproved VM SKU.
 