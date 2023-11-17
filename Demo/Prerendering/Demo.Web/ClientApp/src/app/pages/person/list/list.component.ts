import { Component, Inject, PLATFORM_ID } from '@angular/core';
import { Person } from '../../../entities/person';
import { PersonService } from '../../../services/person.service';
import { Title } from '@angular/platform-browser';
import { isPlatformServer } from '@angular/common';

@Component({
	selector: 'app-person-list',
	templateUrl: './list.component.html',
	styleUrls: ['./list.component.scss']
})
export class PersonListComponent {

	constructor(private personService: PersonService, @Inject(PLATFORM_ID) private platformId: Object, @Inject('PEOPLE') private peopleInj: Person[], private titleService: Title) {
		this.titleService.setTitle('People');
		if (isPlatformServer(platformId)) {
			this.people = peopleInj;
		} else {
			this.loadPeople();
		}
	}

	private loadPeople() {
		this.personService.getPeople(false).subscribe(people => {
			this.people = people;
		});
	}

	people: Person[] = [];

}