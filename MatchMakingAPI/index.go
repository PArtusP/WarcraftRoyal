/*
Copyright 2016 The Kubernetes Authors.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

// Note: the example only works with the code within the same release/branch.
package main

import (
	"encoding/json"
	"flag"
	"fmt"
	"net/http"
	"os"
	"path/filepath"

	"agones.dev/agones/pkg/client/clientset/versioned"
	"k8s.io/client-go/tools/clientcmd"
	//
	// Uncomment to load all auth plugins
	// _ "k8s.io/client-go/plugin/pkg/client/auth"
	//
	// Or uncomment to load specific auth plugins
	// _ "k8s.io/client-go/plugin/pkg/client/auth/azure"
	// _ "k8s.io/client-go/plugin/pkg/client/auth/gcp"
	// _ "k8s.io/client-go/plugin/pkg/client/auth/oidc"
)

func main() {

	//version := "latest"
	listenAddr := ":8080"
	if val, ok := os.LookupEnv("FUNCTIONS_CUSTOMHANDLER_PORT"); ok {
		listenAddr = ":" + val
	}
	//kubeconfig = flag.String("kubeconfig", "C:\\Users\\user\\Documents\\GO\\k8s\\.kube\\config", "absolute path to the kubeconfig file")
	// use the current context in kubeconfig
	agonesClient := GetAgonesClient()

	// http.HandleFunc("/servers", func(w http.ResponseWriter, r *http.Request) {
	// 	// Convert the byte slice to a string and print it
	// 	CRUD_Server_Index(agonesClient, w)
	// })

	http.HandleFunc("/api/findserver", func(w http.ResponseWriter, r *http.Request) {

		var info PlayerGameInfo
		err := json.NewDecoder(r.Body).Decode(&info)
		if err != nil {
			http.Error(w, err.Error(), http.StatusBadRequest)
			return
		}

		// if info.GameVersion != version {
		// 	http.Error(w, "Wrong game version", http.StatusBadRequest)
		// } else {
		CRUD_Server_MMFind(agonesClient, w)
		//}

	})

	if err := http.ListenAndServe(listenAddr, nil); err != nil {
		panic(err)
	}

	fmt.Printf("Serving API\n")
}

func GetAgonesClient() *versioned.Clientset {
	var kubeconfig *string
	//if home := homedir.HomeDir(); home != "" {
	kubeconfig = flag.String("kubeconfig", filepath.Join(".kube", "config"), "(optional) absolute path to the kubeconfig file")
	//} else {
	//kubeconfig = flag.String("kubeconfig", "root\\.kube\\config", "absolute path to the kubeconfig file")
	//}

	flag.Parse()

	config, err := clientcmd.BuildConfigFromFlags("", *kubeconfig)
	if err != nil {
		panic(err.Error())
	}

	agonesClient, err := versioned.NewForConfig(config)
	if err != nil {
		panic(err.Error())
	}
	return agonesClient
}
