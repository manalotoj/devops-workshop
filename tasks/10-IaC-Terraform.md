## Terraform and Azure DevOps Hands-On

In this section, you will use Terraform (tf) to configure Azure IaaS services. In the end, you will establish a 3-tier IaaS environment consisting of the following:
- Virtual network with 4 subnets
- frontend Windows server VM with web role enabled
- middle-tier Windows server VM with web role enabled
- SQL Server VM
- Windows jumpbox VM (note: in practice, consider using [Azure Bastion](https://azure.microsoft.com/en-us/services/azure-bastion/) based on availability)

### Task #1: Create a resource group using Terraform
In this task, you will create a base resource group. Resources within this base resource group will be referenced in latter taks.

1. Add a folder named tf within the DevOps repo to store all of your Terraform (tf) files
2. Within the tf folder, create a folder named base.
3. Within the base folder, create a file named main.tf.
4. Copy and save the following contents:
``` terraform
resource "azurerm_resource_group" "base" {
  name     = "base-[your-initials]"
  location = "westus2"

  tags = {
    environment = "Development"
  }
}
```
5. In a command shell, run the following scripts and observer the output and results after each line. 
``` terraform
terraform init

terraform plan

terraform apply --auto-approve
```
if using VS Code, follow instructions from https://docs.microsoft.com/en-us/azure/terraform/terraform-vscode-extension.
5.. Delete the resource group by running terraform destroy. Observe the output verify the results.
6. Recreate the resource group by running terraform apply --auto-approve. Observe the output and verify the results. 

 ### Task 2: Import existing resources
Terraform currently lacks the ability to fully reverse engineer existing infrastructure. The alternative is a 2-step process, 1) import state, and 2) manually author the corresponding configuration(s).

1. In the azure portal, navigate to the resource group created in step 1 and create a storage account. Specify the following attributes:
     - storage account name:	statestore[your-initials]
     - location	westus2
     - replication	locally-redundant storage (LRS)
	
2. Modify main.tf by appending the following resource:
``` terraform
	resource "azurerm_storage_account" "import" {
	  name                      = "statestore[your-initials]"
	  resource_group_name       = "${azurerm_resource_group.main.name}"
	  location                  = "westus2"
	  account_kind              = "StorageV2"
	  account_tier              = "Standard"
	  account_replication_type  = "LRS"
	  enable_https_traffic_only = true
	}
```
3. In a command shell, run terraform plan

*** note that terraform is not aware of the storage account and wants to add it; not what you want

  - import the storage account by executing the following
	terraform import azurerm_resource_group.import [storage-account-resource-id]
  - this imports the state
	- if you run into obscure errors (like 404), update your version of terraform, restart VS Code; alternatively, execute these steps from the portal using an Azure cloud shell.
  - Run terraform plan to verify that the storage account has been imported.

### Task 3: 3-Tier IaaS Environment
In this task, you will create a 3-tier IaaS environment consisting of the following:
   * virtual network with 3 subnets (web subnet, middle tier subnet, backend subnet, management subnet)
   * Network security group (NSG)
   * Frontend web server (Windows Server 2016 w/ web role enabled)
   * Middle tier server (same as frontend)
   * WIndows Server 2016 with SQL Server 2016
   * Access will be locked down via NSG rules:

In this task, we will maintain tf state in the Azure storage account created in Task 2.

1. Use the following Azure CLI script to create containers.

```
az storage container create --account-name <storage-account-name> --name state-dev

az storage container create --account-name <storage-account-name> --name state-stg
```

2. In your repository, create folder named main at the same level as the base folder and create a file named main.tf
3. Add a random string generator configuration. This will be used as a suffix to make service names unique.
``` terraform
resource "random_string" "rnd" {
  length    = 4
  special   = false
  upper     = false
  number    = false
}
```

4. Save tf state in the previously created Azure storage account using the following:
``` terraform
	terraform {
	  required_version = ">=0.12"
	  backend "azurerm" {
	    storage_account_name  = <storage-account-name>"
	    container_name        = "state-dev"
	    key                   = "key-dev"
	    access_key            = "<access-key>"
	  }
	}
```
Retrieve the storage access key from the portal or via script:
``` azcli
az storage account keys list -g <resoure-group-name> -n <storage-account-name>
``` azcli
5. Add a resource group named dev-main-<random suffix> as shown:
``` terraform
resource "azurerm_resource_group" "main" {
  name     = "dev-main-${random_string.rnd.result}"
  location = "westus2"

  tags = {
    environment = "Development"
  }
}
```

6. Create a virtual network (VNET) configuration and save it in a file named vnet.tf. The VNET must be configured as follows:
- vnet cidr: 10.0.0.0/22
- subnets:
  - management-subnet cidr: 10.0.0.0/24
  - frontend-subnet cidr: 10.0.1.0/24
  - middletier-subnet cidr: 10.2.0.0/24
  - backend-subnet cidr: 10.0.3.0/24

Use the following general configuration for reference.
``` terraform
	resource "azurerm_virtual_network" "vnet" {
	  name                = "<vnet-name>"
	  location            = azurerm_resource_group.main.location
	  resource_group_name = azurerm_resource_group.main.name
	  address_space       = ["<vnet-cidr-block>"]
	}
	
	resource "azurerm_subnet" "web" {
	  name                  = "<subnet1-name>"
	  resource_group_name   = azurerm_resource_group.main.name
	  virtual_network_name  = azurerm_virtual_network.vnet.name
	  address_prefix        = "<subnet1-cidr-block>"
	}

	resource "azurerm_subnet" "management" {
	  name                  = "<subnet2-name>"
	  resource_group_name   = azurerm_resource_group.main.name
	  virtual_network_name  = azurerm_virtual_network.vnet.name
	  address_prefix        = "<subnet-2-cidr-block>"
	}

... # more subnet definitions go here	
```

7. Create network security group (NSG) configuration and save it in a file named nsg.tf. The NSG must be configured as follows:
  - Only the web subnet will allow inbound traffic on port 80
  - Only the management subnet will allow inbound traffic on 3389
  - No inbound traffic will be permitted to the backend subnet

Use the following general NSG configuration for reference:
``` terraform
	resource "azurerm_network_security_group" "default" {
	  name                = "<nsg-name>"
	  location            = azurerm_resource_group.test.location
	  resource_group_name = azurerm_resource_group.test.name
	}
	
	# allow http/https
	resource "azurerm_network_security_rule" "allow_http" {
	  name                        = "allow-http"
	  priority                    = 100
	  direction                   = "Inbound"
	  access                      = "Allow"
	  protocol                    = "Tcp"
	  source_port_range           = "*"
	  destination_port_range      = "80"
	  source_address_prefix       = "<your-ip-address>"
	  destination_address_prefix  = "<frontend-cidr-block>"
	  resource_group_name         = azurerm_resource_group.test.name
	  network_security_group_name = azurerm_network_security_group.default.name
	}
  ... more rules go here
```

8. Create a Windows Server 2016 with IIS role enabled and configured to serve both asp.net and asp.net core applications. Place this server in the frontend-vnet. 
  - Use the <root>/terraform/vm-frontend.tf** configuration for reference.
  - ***Note**: The reference file includes VM username and password for this exercise. Obviously, this is a poor practice. This will be corrected in a latter exercise.*
TODO: add link to winvm.tf file

9. Repeat step 8 and place the virtual machine in the middletier-subnet.

10.   Create a Windows Server with SQL Server 2016.    
  - Place this server in the backend-vnet.
  - Use the <root>/terraform/vm-sql.tf** configuration for reference.

11.  Create a jumpbox
	- Create a configuration file that will deploy a jumpbox into the management-subnet. 

12. In a command shell, run the following scripts and observer the output and results after each line. You may have to repeat ***terraform plan*** and/or ***terraform apply*** if you run into any issues.
``` terraform
terraform init

terraform plan

terraform apply --auto-approve
```
- Verify the following:
  - You are able to rdp into the jumpbox
From within the jumpbox, you are able to access the SQL Server VM as well as each of the 2 Windows Server VMs.
	- From within the jumpbox, verify that the frontend and backend VMs accept internal, inbound web traffic on port 80.
	- Verify that you cannot rdp into any other VM.
	- Verify that the frontend VM accepts external, inbound web traffic on port 80.
	- Verify that the middletier VM does not allow external, inbound web traffic on port 80.

13. Destroy your deployment.
Once you have verified that your deployment is in working order, destroy it using ***terraform destroy --auto-approve***.

14. Optional Tasks
  - Use variables to remove username and password from configuration files
  - Replace all values with variables. This will make each module/component easier to test.
  - Convert your vnet configuration into a reusable module.
  - Place web VMs in an availability set and front with a load balancer.
  - Test your configurations using [Terratest](https://docs.microsoft.com/en-us/azure/terraform/terratest-in-terraform-modules).
