#pragma description ArcDb database file format

#pragma endian big

#include <std/io.pat> 
#include <std/mem.pat> 
#include <std/math.pat> 

struct Folio {
  u8 Hash[20];
};

Folio first @ 0x00;