byte serialDatas[3];
int DataIdx;
bool isEffect;

byte effect[3] = {0xFA, 0x55, 0x55};
byte invald[3] = {0xFA, 0x7F, 0x7F};

unsigned long ptime = 0;

void setup() {
  // put your setup code here, to run once:
	Serial.begin(9600);

	serialDatas[0] = 0;
	serialDatas[1] = 0;
	serialDatas[2] = 0;

	DataIdx = 0;
	isEffect = false;
	

}

void loop() {
  // put your main code here, to run repeatedly:
	while (Serial.available() >= 3)
	{
		serialDatas[DataIdx] = Serial.read();
		if (serialDatas[DataIdx] & 0x80)
		{      
			DataIdx++;
			while (DataIdx <= 2)
			{
				serialDatas[DataIdx] = Serial.read();
				if ((serialDatas[DataIdx] & 0x01) == 0x00) DataIdx++;
				else break;
			}
			if (DataIdx == 3)
			{
				switch (serialDatas[0] >> 4)
				{
				case 0xE:
					GetADVal();
					break;
				case 0x9:
					IOSet();
					break;
				case 0xC:
					IORead();
					break;
				case 0xD:
					PWM();
					break;
				case 0xF:
					Other();
					break;
				default:
					break;
				}
			}
			if (isEffect) ret_Effect();
			else ret_Invald();
			DataIdx = 0;
			isEffect = false;
		}
	}

  unsigned long ctime = millis();
  
  if (ctime - ptime >= 200)
  {
	  ret_LightV();
	  ret_Temp();
    ptime = ctime;
  }

}

void GetADVal()
{
	if ((byte)(serialDatas[0] == 0xE0))
	{
		double Vcc = 5, R5 = 10000, Vref = 5, T0 = 25 + 273.15, B = 3435, R0 = 10000;
		double ADCdata = analogRead(0);
		double Vtemp = ADCdata * (Vref / 1024);
		double R9 = Vcc * (R5 / Vtemp) - R5;
		double Tx = 1 / (log(R9 / R0) / B + 1 / T0);
		double temp = Tx - 237.15;
		serialDatas[1] = (byte)temp << 1;
		serialDatas[2] = (byte)temp << 7;
		ret_Data();
		isEffect = true;
	}
	else if((byte)(serialDatas[0] == 0xE1))
	{
		double v = analogRead(A1);
		v = v * 5 / 1024;
		serialDatas[1] = (byte)v << 1;
		serialDatas[2] = (byte)v << 7;
		ret_Data();
		isEffect = true;
	}


}

void IOSet()
{
	int pin = (int)(serialDatas[0] & 0x0F);
	if (serialDatas[1] == 0x01)
	{
		analogWrite(pin, HIGH);
		isEffect = true;
	}

	else if (serialDatas[1] == 0x00)
	{
		analogWrite(pin, LOW);
		isEffect = true;
	}
}

void IORead()
{
	int pin = (int)(serialDatas[0] & 0x07);
	if (digitalRead(pin) == HIGH)
	{
		Serial.write(1);
		isEffect = true;
	}
	else if (digitalRead(pin) == LOW)
	{
		Serial.write(0);
		isEffect = true;
	}
}

void PWM()
{
	if ((serialDatas[2] & 0x04) == 0x04)  
	{
		byte val = analogRead(serialDatas[0] & 0x0F);
		serialDatas[1] = (val >> 3) & 0x7F;
		serialDatas[2] = (val & 0x07) << 5;
		ret_Data();
	}
	else
	{
		int val = (int)serialDatas[1] + (serialDatas[2] >> 7);
		analogWrite(serialDatas[0] & 0x0F, val);
	}
	isEffect = true;
}

void ret_Temp()
{
	int temp = analogRead(A0);

	byte byte0 = 0xE3;
  byte byte1 = byte(temp >> 3) & 0x7F;
  byte byte2 = (byte(temp) & 0x07) << 5;
	Serial.write(byte0);
	Serial.write(byte1);
	Serial.write(byte2);
}

void ret_LightV()
{
	int v = analogRead(A1);

	byte byte0 = 0xE4;
  byte byte1 = byte(v >> 3) & 0x7F;
  byte byte2 = (byte(v) & 0x07) << 5;
	Serial.write(byte0);
	Serial.write(byte1);
	Serial.write(byte2);
}

void Other()
{
	if (serialDatas[0] == 0xFF)
	{
		int j = millis();
		Serial.write(j);
		isEffect = true;
	}
	else
	{
		
	}
}

void ret_Effect()
{
	for (int i = 0; i < 3; i++)
	{
		Serial.write(effect[i]);
	}
}

void ret_Invald()
{
	for (int i = 0; i < 3; i++)
	{
		Serial.write(invald[i]);
	}
}

void ret_Data()
{
	for (int i = 0; i < 3; i++)
	{
		Serial.write(serialDatas[i]);
	}
}
