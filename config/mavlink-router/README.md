# MAVLink Router ([repo](https://github.com/mavlink-router/mavlink-router))

Route mavlink packets between endpoints.

## Build
	
	$ docker build -t mavlink-router .

## Run

	$ docker run -p 3000:3000/udp -v etc:/mavlink-router/etc --name mavlink-router mavlink-router:latest