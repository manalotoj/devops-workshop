resource "azurerm_public_ip" "frontend_pip" {
  name                = "fe-pip"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  allocation_method   = "Static"
}

resource "azurerm_network_interface" "frontend_nic" {
  name                = "fe-nic"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  ip_configuration {
    name                          = "fe-ipconf"
    subnet_id                     = azurerm_subnet.frontend.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.frontend_pip.id    
  }
}

resource "azurerm_virtual_machine" "frontend_vm" {
  name                  = "fe-vm"
  location              = azurerm_resource_group.main.location
  resource_group_name   = azurerm_resource_group.main.name
  network_interface_ids = [azurerm_network_interface.frontend_nic.id]
  vm_size               = "Standard_DS2_v2"

  storage_image_reference {
    publisher = "MicrosoftWindowsServer"
    offer     = "WindowsServer"
    sku       = "2019-Datacenter"
    version   = "latest"
  }
  storage_os_disk {
    name              = "fe-osdisk"
    caching           = "ReadWrite"
    create_option     = "FromImage"
    managed_disk_type = "Standard_LRS"
  }
  os_profile {
    computer_name  = "windows"
    admin_username = "vmuser"
    admin_password = "P@ssword123456"
  }
  os_profile_windows_config {
    provision_vm_agent              = true
    enable_automatic_upgrades       = true
  }
}

  resource "azurerm_virtual_machine_extension" "fe_vm_ext" {
  name                 = "fe-ext"
  location             = azurerm_resource_group.main.location
  resource_group_name  = azurerm_resource_group.main.name
  virtual_machine_name = azurerm_virtual_machine.frontend_vm.name
  publisher            = "Microsoft.Compute"
  type                 = "CustomScriptExtension"
  type_handler_version = "1.9"

  # CustomVMExtension Documetnation: https://docs.microsoft.com/en-us/azure/virtual-machines/extensions/custom-script-windows

  protected_settings = <<PROTECTED_SETTINGS
    {
      "commandToExecute": "powershell Install-WindowsFeature Web-Server,Web-Asp-Net45,NET-Framework-Features;Invoke-WebRequest https://go.microsoft.com/fwlink/?linkid=848827 -outfile $env:temp\\dotnet-dev-win-x64.1.0.6.exe;Start-Process $env:temp\\dotnet-dev-win-x64.1.0.6.exe -ArgumentList '/quiet' -Wait;Invoke-WebRequest https://go.microsoft.com/fwlink/?LinkId=817246 -outfile $env:temp\\DotNetCore.WindowsHosting.exe;Start-Process $env:temp\\DotNetCore.WindowsHosting.exe -ArgumentList '/quiet' -Wait;Stop-Service was -Force;Start-Service w3svc;"
    }
  PROTECTED_SETTINGS
}
