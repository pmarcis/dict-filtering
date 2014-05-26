dict-filtering
==============

Giza++ dictionary filtering tool and (initial) transliteration dictionary acquisition tool

If you are looking for the filtered Giza++ dictionaries for European languages, you can download them from [META-SHARE](http://metashare.tilde.lv/repository/search/?q=giza%2B%2B+dictionaries). If you have issues finding them, drop me a message (will see what we can do).

If you are using the Transliteration-based dictionary filtering method (or any component), please refer to the following paper:

	@InProceedings{AKER14.803,
	
	author = {Ahmet Aker and Monica Paramita and Mārcis Pinnis and Robert Gaizauskas},
	
	title = {Bilingual dictionaries for all EU languages},

	booktitle = {Proceedings of the Ninth International Conference on Language Resources and Evaluation (LREC'14)},

	year = {2014},

	month = {may},

	date = {26-31},

	address = {Reykjavik, Iceland},

	editor = {Nicoletta Calzolari (Conference Chair) and Khalid Choukri and Thierry Declerck and Hrafn Loftsson and Bente Maegaard and Joseph Mariani and Asuncion Moreno and Jan Odijk and Stelios Piperidis},

	publisher = {European Language Resources Association (ELRA)},

	isbn = {978-2-9517408-8-4},

	language = {english}
	
	} 

The paper can be freely accessed from the on-line proceedings of the conference - [here](http://www.lrec-conf.org/proceedings/lrec2014/pdf/803_Paper.pdf).

The dictionary filtering method contains a component that allows extracting transliteration dictionaries from probabilistic dictionaries. The process, if run multiple times (in a bootstrapping approach), allows creating quite large transliteration dictionaries. The method together with a multilingual transliteration dictionary for European languages will be described in an upcoming paper "Bootstrapping of a Multilingual Transliteration Dictionary for European Languages" in the conference [Baltic HLT 2014](http://tekstynas.vdu.lt/hlt2014).

