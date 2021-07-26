# MAVLink Router ([repo](https://github.com/mavlink-router/mavlink-router))

Route mavlink packets between endpoints.

## Build
	
	$ git clone https://github.com/mavlink-router/mavlink-router.git --recursive
	$ cp Aerit.MAVLink/config/mavlink-router/Dockerfile mavlink-router
	$ cd mavlink-router
	$ docker build -t mavlink-router -f Dockerfile.arm32v7 .

## Run

	* Debug:

	$ docker run --name mavlink-router -p 3000:3000/udp -v /home/pablo/source/Aerit.MAVLink/config/mavlink-router/etc:/mavlink-router/etc mavlink-router:latest

	* Drone:

	$ docker run --name mavlink-router --device /dev/ttyAMA1 -p 3000:3000/udp -v /home/pi/etc/mavlink-router:/mavlink-router/etc mavlink-router:latest
