# INFRA

## Build images : 
```
docker build --target GameServer -t jausseran/ageofroyalserver --no-cache . 
docker image push jausseran/ageofroyalserver

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
az aks create --resource-group $AKS_RESOURCE_GROUP --name $AKS_NAME --node-count 1 --generate-ssh-keys --node-vm-size Standard_A4_v2 --kubernetes-version 1.31 --enable-node-public-ip

az aks install-cli      # Install kubectl

# Get credentials for your new AKS cluster
az aks get-credentials --resource-group $AKS_RESOURCE_GROUP --name $AKS_NAME
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
kubectl logs --follow fleet-9qpw4-4s456 -n gameserver
kubectl get gameservers -n gameserver --watch
```

### Cleaning
```
kubectl delete gs tps-server-t64jn -n gameserver
kubectl delete fleet fleet -n gameserver
kubectl delete fas gamefleet-autoscaler -n gameserver
```


