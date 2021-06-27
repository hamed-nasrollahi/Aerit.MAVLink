# MAVLink Router ([repo](https://github.com/mavlink-router/mavlink-router))

Route mavlink packets between endpoints.

## Build
	
	$ docker build -t mavlink-router .

## Run

	$ docker run --name mavlink-router -p 3000:3000/udp -v /home/pablo/source/Aerit.MAVLink/config/mavlink-router/etc:/mavlink-router/etc mavlink-router:latest