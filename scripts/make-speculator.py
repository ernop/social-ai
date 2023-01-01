import os
base="/mnt/c/git/social-ai/images"

def mkline(f):
	term=f.split('_',1)[1].split('_man_from_england_aged_20')[0].replace('_',' ').strip()
	print(term)
	term=term.replace("a ","").replace(" man","").strip()
	res="\r\n<tr><td style='padding:30px;'><h2>"+term+"</h2></td><td><img width=450 src=file:///c:/git/social-ai/images/"+f+"></td> "
	return res

def make():
	files=os.listdir(base)
	out=open('out.html','w')
	out.write("<table>")
	for f in sorted(files):
		if "_man_from_england" not in f:
			continue
		line=mkline(f)
		out.write(line)
		
	out.write("</table>")
		
	out.close()

make()