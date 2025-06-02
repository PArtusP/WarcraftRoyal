# INFRA

## Build images : 
```
docker build --target GameServer -t jausseran/ageofroyalserver --no-cache . 
docker image push jausseran/ageofroyalserver
docker run -d jausseran/ageofroyalserver

docker build --target MatchMakingAPI -t jausseran/ageofroyalmmapi .
docker image push jausseran/ageofroyalmmapi
docker run -d jausseran/ageofroyalmmapi
```
## Run API local: 
```
cd MatchMakingAPI/AzureFunctions
GOOS=linux GOARCH=amd64 go build ../. 
```
## Run gameserver : 
### Set up AZURE  : 

``` 
## Declare necessary variables, modify them according to your needs
AKS_RESOURCE_GROUP=TPS_RG           # Name of the resource group your AKS cluster will be created in
AKS_NAME=ageofroyal_cluster                # Name of your AKS cluster
AKS_LOCATION=centralfrance          # Azure region in which you'll deploy your AKS cluster
AZUREFUNC_NAME=AgeOfRoyal-MMAPI          # Azure region in which you'll deploy your AKS cluster  

## az login # Login to azure 

# Create the Resource Group
az group create --name $AKS_RESOURCE_GROUP --location $AKS_LOCATION

# Create 1 node AKS cluster. Node size : Standard A1 v2 and Kub v1.28. SSH keys will be generated
az aks create --resource-group $AKS_RESOURCE_GROUP --name $AKS_NAME --node-count 1 --generate-ssh-keys --node-vm-size Standard_A4_v2 --kubernetes-version 1.32 --enable-node-public-ip

az aks install-cli      # Install kubectl

# Get credentials for your new AKS cluster
az aks get-credentials --resource-group $AKS_RESOURCE_GROUP --name $AKS_NAME

# Get config to copy/paste
az aks get-credentials --resource-group $AKS_RESOURCE_GROUP --name $AKS_NAME --file MatchMakingAPI/AzureFunctions/.kube/config


RESOURCE_GROUP_WITH_AKS_RESOURCES=MC_TPS_RG_ageofroyal_cluster_francecentral        # Azure namespace
NSG_NAME=aks-agentpool-22065260-nsg

###   Security group 
az network nsg rule create \
  --resource-group $RESOURCE_GROUP_WITH_AKS_RESOURCES \
  --nsg-name $NSG_NAME \
  --name AgonesUDP \
  --access Allow \
  --protocol Udp \
  --direction Inbound \
  --priority 520 \
  --source-port-range "*" \
  --destination-port-range 7000-8000
``` 

### Install agones : 
```
helm repo add agones https://agones.dev/chart/stable
helm repo update
helm install agones --namespace agones-system --create-namespace agones/agones --set agones.featureGates=PlayerTracking=true&StateAllocationFilter=true&PlayerAllocationFilter=true
kubectl create namespace gameserver
kubectl create serviceaccount agones-sdk -n gameserver
kubectl apply -f agones-sdk-clusterrole.yaml
kubectl apply -f agones-sdk-clusterrolebinding.yaml
```

### Creation components
 - Gameserver
   - `kubectl create -f gameserver.yaml --namespace gameserver`
 - Fleet
   - `kubectl create -f fleet.yaml --namespace gameserver`
 - Fleet autoscaler
   - `kubectl create -f fleetautoscaler.yaml --namespace gameserver`


### Logs
```
kubectl logs --follow fleet-tpkfc-ntmhd -n gameserver
kubectl logs --follow fleet-tpkfc-ntmhd -n gameserver -c ageofroyal-server
kubectl logs --follow fleet-tpkfc-ntmhd -n gameserver -c agones-gameserver-sidecar 
kubectl get gameservers -n gameserver --watch
kubectl get fleet -n gameserver

```

### Cleaning
```
kubectl delete gs fleet-tpkfc-ntmhd -n gameserver
kubectl delete fleet fleet -n gameserver
kubectl delete fas gamefleet-autoscaler -n gameserver
```
 