# !/bin/bash

# This script will populate a wikipedia index with both animals, technology and country info
# for at least 20 of each.

# function which takes an array as argument and loops through it
function loop_through_array() {

    tagType=$1
    linkArray=($@)

    for i in "${linkArray[@]}"; do
        echo "Creating knowledge about: $i"
        tagName=$(echo $i | tr "/" "\n" | tail -n 1)
        # url="https://localhost:7149/url?url=https%3A%2F%2Fen.wikipedia.org%2Fwiki%2F$tagName&index=wikipedia&tag=animal%3A$tagName&tag=language%3Aen"
        url="https://localhost:7149/url?url=https%3A%2F%2Fen.wikipedia.org%2Fwiki%2F$tagName&index=wikipedia&tag=$tagType%3A$tagName&tag=language%3Aen"

        echo "Tagname: $tagName"
        echo "URL: $url"

        curl -X 'POST' $url -H 'accept: application/json' -d ''

        echo ""
        echo ""

        # sleep for 1 second
        sleep 1
    done
}

################ Animals ################
animals=(
    "https://en.wikipedia.org/wiki/Lion"
    "https://en.wikipedia.org/wiki/Tiger"
    "https://en.wikipedia.org/wiki/Bear"
    "https://en.wikipedia.org/wiki/Elephant"
    "https://en.wikipedia.org/wiki/Giraffe"
    "https://en.wikipedia.org/wiki/Whale"
    "https://en.wikipedia.org/wiki/Shark"
    "https://en.wikipedia.org/wiki/Seal"
    "https://en.wikipedia.org/wiki/Penguin"
    "https://en.wikipedia.org/wiki/Parrot"
    "https://en.wikipedia.org/wiki/Chimpanzee"
    "https://en.wikipedia.org/wiki/Gorilla"
    "https://en.wikipedia.org/wiki/Orangutan"
    "https://en.wikipedia.org/wiki/Bat"
)

loop_through_array "animal" ${animals[@]}
################ Animals ################

################ Technology ################
technology=(
    "https://en.wikipedia.org/wiki/Computer"
    "https://en.wikipedia.org/wiki/Smartphone"
    "https://en.wikipedia.org/wiki/Television"
    "https://en.wikipedia.org/wiki/Internet"
    "https://en.wikipedia.org/wiki/Robot"
    "https://en.wikipedia.org/wiki/Artificial_intelligence"
    "https://en.wikipedia.org/wiki/Spacecraft"
    "https://en.wikipedia.org/wiki/Automobile"
    "https://en.wikipedia.org/wiki/Airplane"
    "https://en.wikipedia.org/wiki/Train"
    "https://en.wikipedia.org/wiki/Ship"
    "https://en.wikipedia.org/wiki/Space_station"
    "https://en.wikipedia.org/wiki/Drone"
    "https://en.wikipedia.org/wiki/3D_printing"
    "https://en.wikipedia.org/wiki/Quantum_computing"
    "https://en.wikipedia.org/wiki/Blockchain"
    "https://en.wikipedia.org/wiki/Cloud_computing"
    "https://en.wikipedia.org/wiki/Big_data"
)

loop_through_array "technology" ${technology[@]}
################ Technology ################

################ Countries ################
countries=(
    "https://en.wikipedia.org/wiki/United_States"
    "https://en.wikipedia.org/wiki/Canada"
    "https://en.wikipedia.org/wiki/Mexico"
    "https://en.wikipedia.org/wiki/China"
    "https://en.wikipedia.org/wiki/Japan"
    "https://en.wikipedia.org/wiki/India"
    "https://en.wikipedia.org/wiki/United_Kingdom"
    "https://en.wikipedia.org/wiki/France"
    "https://en.wikipedia.org/wiki/Germany"
    "https://en.wikipedia.org/wiki/Italy"
    "https://en.wikipedia.org/wiki/Russia"
    "https://en.wikipedia.org/wiki/Brazil"
    "https://en.wikipedia.org/wiki/Australia"
    "https://en.wikipedia.org/wiki/South_Africa"
    "https://en.wikipedia.org/wiki/Egypt"
    "https://en.wikipedia.org/wiki/South_Korea"
    "https://en.wikipedia.org/wiki/Thailand"
    "https://en.wikipedia.org/wiki/Sweden"
)

loop_through_array "country" ${countries[@]}
################ Countries ################
