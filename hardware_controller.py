w1387769 Mohammed Rahim Baraky
import RPi.GPIO as GPIO, threading, os, time, sys, subprocess


#Setting the pins for the motors. A single motor as two pin outs
#allowing you to change the polarity which lets you change the direction
#it spins. The PiRoCon shield i'm using is 24,26 for left motor and
#19,21 for the right.
Right1 = 24
Right2 = 26

Left1 = 19
Left2 = 21

#Setting the forward and rear infrared sensors
IRBack = 7
IRFront = 11

def initialise():
	#created some global var's for the motors, ML1 = Motor Left 1
    global ML1, ML2, MR1, MR2
	#Setting how the IO pins will be numbered. GPIO.BOARD is the 
	#numbers printed on the board. An alternate setmode is GPIO.BCM
	#which uses the channel numbers. 
    GPIO.setmode(GPIO.BOARD)
	
	#Setting up the IR inputs so we can read value of the GPIO pin
    GPIO.setup(IRBack, GPIO.IN)
    GPIO.setup(IRFront, GPIO.IN)
	
	#Setting up the motors and then enabling pwm
    GPIO.setup(Left1, GPIO.OUT)
    ML1 = GPIO.PWM(Left1, 20)
    ML1.start(0)

    GPIO.setup(Left2, GPIO.OUT)
    ML2 = GPIO.PWM(Left2, 20)
    ML2.start(0)

    GPIO.setup(Right1, GPIO.OUT)
    MR1 = GPIO.PWM(Right1, 20)
    MR1.start(0)

    GPIO.setup(Right2, GPIO.OUT)
    MR2 = GPIO.PWM(Right2, 20)
    MR2.start(0)

def cleanup():
    stop()
    GPIO.cleanup()

def irBack():
    if GPIO.input(IRBack)==0:
        return True
    else:
        return False
    
def irFront():
    if GPIO.input(IRFront)==0:
        return True
    else:
        return False
######################################################################	
def stop():
    ML1.ChangeDutyCycle(0)
    ML2.ChangeDutyCycle(0)
    MR1.ChangeDutyCycle(0)
    MR2.ChangeDutyCycle(0)
    
# forward(speed): Sets both motors to move forward at speed. 0 <= speed <= 100
def forward(speed):
    ML1.ChangeDutyCycle(speed)
    ML2.ChangeDutyCycle(0)
    MR1.ChangeDutyCycle(speed)
    MR2.ChangeDutyCycle(0)
    ML1.ChangeFrequency(speed)
    MR1.ChangeFrequency(speed)
    
# reverse(speed): Sets both motors to reverse at speed. 0 <= speed <= 100
def reverse(speed):
    ML1.ChangeDutyCycle(0)
    ML2.ChangeDutyCycle(speed)
    MR1.ChangeDutyCycle(0)
    MR2.ChangeDutyCycle(speed)
    ML1.ChangeFrequency(speed)
    MR1.ChangeFrequency(speed)

# spinLeft(speed): Sets motors to turn opposite directions at speed. 0 <= speed <= 100
def turnLeft(speed):
    ML1.ChangeDutyCycle(0)
    ML2.ChangeDutyCycle(speed)
    MR1.ChangeDutyCycle(speed)
    MR2.ChangeDutyCycle(0)
    ML2.ChangeFrequency(speed)
    MR1.ChangeFrequency(speed)
    
# spinRight(speed): Sets motors to turn opposite directions at speed. 0 <= speed <= 100
def turnRight(speed):
    ML1.ChangeDutyCycle(speed)
    ML2.ChangeDutyCycle(0)
    MR1.ChangeDutyCycle(0)
    MR2.ChangeDutyCycle(speed)
    ML1.ChangeFrequency(speed)
    MR2.ChangeFrequency(speed)
    
